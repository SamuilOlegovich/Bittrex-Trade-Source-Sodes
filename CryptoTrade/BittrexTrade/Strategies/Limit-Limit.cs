using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using CryptoTrade.BaseTypes;

namespace CryptoTrade.Strategies
{
    public class InternalParamLimit_Limit : StrategyParam
    {
        public ExTool.StepRepresent EnterDistance { get; set; } = new ExTool.StepRepresent(1);
        public ExTool.StepRepresent CloseDistance { get; set; } = new ExTool.StepRepresent(1);
        public bool IsLong { get; set; }
        public bool IsShort { get; set; }
        public ExTool.StepRepresent StopDistance { get; set; } = new ExTool.StepRepresent(1);

        public InternalParamLimit_Limit() : base()
        {
            base.StrategyType = "Limit-Limit";
            Description.Add("EnterDistance", "Расcтояние входа в позицию: [Шаг в %]-[Шаг в пунктах]-[Использовать шаг в %]");
            Description.Add("CloseDistance", "Расcтояние закрытия позиции: [Шаг в %]-[Шаг в пунктах]-[Использовать шаг в %]");
            Description.Add("IsLong", "Делать покупку [true/false]");
            Description.Add("IsShort", "Делать продажу [true/false]");
            Description.Add("StopDistance", "Расcтояние ограничения убытка по стопу: [Шаг в %]-[Шаг в пунктах]-[Использовать шаг в %]");
        }

        public override void GetData(ref Dictionary<string, string> DataLParams)
        {
            base.GetData(ref DataLParams);
            DataLParams.Add("EnterDistance", EnterDistance.StringRepresent());
            DataLParams.Add("CloseDistance", CloseDistance.StringRepresent());
            DataLParams.Add("IsLong", IsLong.ToString());
            DataLParams.Add("IsShort", IsShort.ToString());
            DataLParams.Add("StopDistance", StopDistance.StringRepresent());
        }

        public override void LoadData(Dictionary<string, string> dict)
        {
            base.LoadData(dict);
            EnterDistance = ExTool.StepRepresent.LoadFromString(dict["EnterDistance"]);
            CloseDistance = ExTool.StepRepresent.LoadFromString(dict["CloseDistance"]);
            IsLong = Convert.ToBoolean(dict["IsLong"]);
            IsShort = Convert.ToBoolean(dict["IsShort"]);
            StopDistance = ExTool.StepRepresent.LoadFromString(dict["StopDistance"]);
        }
    }

    public class InternalStateLimit_Limit : StrategyState
    {
        public int PosDirection { get; set; }

        public double EnterLongPrice { get; set; }
        public double EnterShortPrice { get; set; }

        public double ClosePositionPrice { get; set; }
        public double ClosePositionSum { get; set; }
        public double StopPositionPrice { get; set; }

        public InternalStateLimit_Limit() : base()
        {
            Description.Add("PosDirection", "Текущая позиция: 1 - Long; 0 - None; -1 - Short");
            Description.Add("EnterLongPrice", "Цена входа в Long");
            Description.Add("EnterShortPrice", "Цена входа в Short");

            Description.Add("ClosePositionPrice", "Цена закрытия позиции");
            Description.Add("ClosePositionSum", "Сума закрытия позиции");
            Description.Add("StopPositionPrice", "Цена закрытия позиции по стоп-ордеру");
        }

        public override void GetData(ref Dictionary<string, string> DataLParams)
        {
            base.GetData(ref DataLParams);
            DataLParams.Add("PosDirection", PosDirection.ToString());
            DataLParams.Add("EnterLongPrice", EnterLongPrice.ToString());
            DataLParams.Add("EnterShortPrice", EnterShortPrice.ToString());

            DataLParams.Add("ClosePositionPrice", ClosePositionPrice.ToString());
            DataLParams.Add("ClosePositionSum", ClosePositionSum.ToString());
            DataLParams.Add("StopPositionPrice", StopPositionPrice.ToString());
        }

        public override void LoadData(Dictionary<string, string> dict)
        {
            base.LoadData(dict);
            PosDirection = Convert.ToInt32(dict["PosDirection"]);
            EnterLongPrice = Convert.ToDouble(dict["EnterLongPrice"].Replace(',', '.'), CultureInfo.InvariantCulture);
            EnterShortPrice = Convert.ToDouble(dict["EnterShortPrice"].Replace(',', '.'), CultureInfo.InvariantCulture);

            ClosePositionPrice = Convert.ToDouble(dict["ClosePositionPrice"].Replace(',', '.'), CultureInfo.InvariantCulture);
            ClosePositionSum = Convert.ToDouble(dict["ClosePositionSum"].Replace(',', '.'), CultureInfo.InvariantCulture);
            StopPositionPrice = Convert.ToDouble(dict["StopPositionPrice"].Replace(',', '.'), CultureInfo.InvariantCulture);
        }

        public override void Reset()
        {
            base.Reset();
            PosDirection = 0;
            EnterLongPrice = 0;
            EnterShortPrice = 0;
            ClosePositionPrice = 0;
            ClosePositionSum = 0;
            StopPositionPrice = 0;
        }
    }

    public class StrategyLimit_Limit : Strategy
    {
        public sealed override event Action<bool, bool, string> ChangeState; //параметры: bool FromRun(or Stop), bool Result (good or bad), StrategyName
        public sealed override event EventHandler<ActiveOrdersGridEventArgs> ChangeActiveOrders;

        public override IStrategyParam Param => LParam;
        public override IStrategyState State => LState;

        public InternalParamLimit_Limit LParam { get; }
        public InternalStateLimit_Limit LState { get; }
        private readonly object LockLTicker = new object();
        private Ticker LastTicker = null;

        public StrategyLimit_Limit(string uniqueID) : base(uniqueID)
        {
            LParam = new InternalParamLimit_Limit();
            LState = new InternalStateLimit_Limit();

            CStrategyTrade = new StrategyTrade(this, Print);
            CStrategyPrices = new StrategyPrices(this, Print)
            {
                OnUpdateTicker = OnUpdateTicker
            };
        }

        private void OnUpdateTicker(Ticker ticker)
        {
            lock (LockLTicker)
            {
                LastTicker = ticker;
            }

            if (LState.PosDirection == 0 && LParam.IsLong && (double)ticker.Ask < LState.EnterLongPrice)   //enter Long
            {
                var tresult = CStrategyTrade.ExecuteByMarket(true, -1).Result;
                if (tresult != null)
                {
                    LState.PosDirection = 1;
                    LState.ClosePositionSum = (double)tresult.BaseQty;
                    double avgPrice = (double)tresult.AveragePrice;
                    LState.ClosePositionPrice = ExTool.IncreasePrice(avgPrice, LParam.CloseDistance);
                    LState.StopPositionPrice = ExTool.DecreasePrice(avgPrice, LParam.StopDistance);

                    Print(String.Format("Выполнено вход в позицию Long на суму: {0} {1} по цене: {2}. ({3} {4})",
                      tresult.BaseQty, LParam.Market.BaseCurrency, avgPrice,
                     tresult.MarketQty, LParam.Market.MarketCurrency));
                }
            }

            if (LState.PosDirection == 0 && LParam.IsShort && (double)ticker.Bid > LState.EnterLongPrice)  //enter Short
            {
                var tresult = CStrategyTrade.ExecuteByMarket(false, -1).Result;
                if (tresult != null)
                {
                    LState.PosDirection = -1;
                    LState.ClosePositionSum = (double)tresult.BaseQty;
                    double avgPrice = (double)tresult.AveragePrice;
                    LState.ClosePositionPrice = ExTool.DecreasePrice(avgPrice, LParam.CloseDistance);
                    LState.StopPositionPrice = ExTool.IncreasePrice(avgPrice, LParam.StopDistance);

                    Print(String.Format("Выполнено вход в позицию Short на суму: {0} {1} по цене: {2}. ({3} {4})",
                      tresult.BaseQty, LParam.Market.BaseCurrency, avgPrice,
                     tresult.MarketQty, LParam.Market.MarketCurrency));
                }
            }

            if (LState.PosDirection == 1 && (double)ticker.Bid > LState.ClosePositionPrice)     //close Long
            {
                var tresult = CStrategyTrade.ExecuteByMarket(false, LState.ClosePositionSum).Result;
                if (tresult != null)
                {
                    double avgPrice = (double)tresult.AveragePrice;
                    FirstInit(avgPrice);

                    Print(String.Format("Позиция Long закрылась на суму: {0} {1} по цене: {2}. ({3} {4})",
                      tresult.BaseQty, LParam.Market.BaseCurrency, avgPrice,
                     tresult.MarketQty, LParam.Market.MarketCurrency));
                }
            }

            if (LState.PosDirection == 1 && (double)ticker.Ask < LState.ClosePositionPrice)    //close Short
            {
                var tresult = CStrategyTrade.ExecuteByMarket(true, LState.ClosePositionSum).Result;
                if (tresult != null)
                {
                    double avgPrice = (double)tresult.AveragePrice;
                    FirstInit(avgPrice);

                    Print(String.Format("Позиция Short закрылась на суму: {0} {1} по цене: {2}. ({3} {4})",
                      tresult.BaseQty, LParam.Market.BaseCurrency, avgPrice,
                     tresult.MarketQty, LParam.Market.MarketCurrency));
                }
            }

            if (LState.PosDirection != 0 && LState.StopPositionPrice != 0)   //check Stop
            {
                if (LState.PosDirection == 1)
                {
                    if ((double)ticker.Ask < LState.ClosePositionPrice)  //close Long by stop
                    {
                        var tresult = CStrategyTrade.ExecuteByMarket(false, LState.ClosePositionSum).Result;
                        if (tresult != null)
                        {
                            double avgPrice = (double)tresult.AveragePrice;
                            FirstInit(avgPrice);
                            
                            Print(String.Format("Позиция Long закрылась по стопу на суму: {0} {1} по цене: {2}. ({3} {4})",
                            tresult.BaseQty, LParam.Market.BaseCurrency, avgPrice,
                                 tresult.MarketQty, LParam.Market.MarketCurrency));
                        }
                    }
                }

                if (LState.PosDirection == -1)
                {
                    if ((double)ticker.Bid > LState.ClosePositionPrice)  //close Short by stop
                    {
                        var tresult = CStrategyTrade.ExecuteByMarket(true, LState.ClosePositionSum).Result;
                        if (tresult != null)
                        {
                            double avgPrice = (double)tresult.AveragePrice;
                            FirstInit(avgPrice);

                            Print(String.Format("Позиция Short закрылась по стопу на суму: {0} {1} по цене: {2}. ({3} {4})",
                            tresult.BaseQty, LParam.Market.BaseCurrency, avgPrice,
                                 tresult.MarketQty, LParam.Market.MarketCurrency));
                        }
                    }
                }
            }
        }

        public override void Start(bool AtFirst)
        {
            try
            {
                if (AtFirst)
                {
                    LState.Reset();
                    FirstInit();
                }
                else
                {
                    ActiveOrdersInfo();
                }

                if (CStrategyPrices.StartUpdateTicker())
                {
                    LState.IsStartegyRun = true;
                    ChangeState?.Invoke(true, true, LParam.StrategyName);
                }
                else
                {
                    LState.IsStartegyRun = false;
                    ChangeState?.Invoke(true, false, LParam.StrategyName);
                }
                SaveData();
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Error in start: " + ex.Message);
                ChangeState?.Invoke(true, false, LParam.StrategyName);
            }
        }

        private void FirstInit(double avgPrice = -1)
        {
            LState.Reset();

            Ticker ticker = null;
            if (avgPrice == -1)
            {
                var task = Task.Run(() => LParam.Stock.GetMarketPrice(Param.Market, Print));
                ticker = task.Result;
            }

            if (LParam.IsLong)
            {
                double curPrice = avgPrice == -1 ? (double)ticker.Bid : avgPrice;
                LState.EnterLongPrice = ExTool.DecreasePrice(curPrice, LParam.EnterDistance);
            }

            if (LParam.IsShort)
            {
                double curPrice = avgPrice == -1 ? (double)ticker.Ask : avgPrice;
                LState.EnterShortPrice = ExTool.IncreasePrice(curPrice, LParam.EnterDistance);
            }
        }

        public override string ShowInfo(bool NotOnlyParams = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine(base.ShowInfo(false));

            var RecData = new Dictionary<string, string>();
            State.GetData(ref RecData);

            sb.Append("Состояние:\r\n");
            foreach (var t in RecData)
            {
                sb.AppendFormat("{0}: {1}\r\n", t.Key, t.Value);
            }

            return sb.ToString();
        }

        public override void Stop()
        {
            try
            {
                CStrategyPrices.StopUpdates();
                LState.IsStartegyRun = false;

                var ActiveOrdersE = new ActiveOrdersGridEventArgs()
                {
                    StrategyType = LParam.StrategyType,
                    StrategyName = LParam.StrategyName
                };
                ChangeActiveOrders?.Invoke(this, ActiveOrdersE);
                SaveData();
                ChangeState?.Invoke(false, true, LParam.StrategyName);
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print(String.Format("Ошибка при остановке стратегии: {0}", ex.Message), true);
                ChangeState?.Invoke(false, false, LParam.StrategyName);
            }
        }

        public override void ForceStop()
        {
            try
            {
                CStrategyPrices.StopUpdates();
                LState.IsStartegyRun = false;

                var ActiveOrdersE = new ActiveOrdersGridEventArgs()
                {
                    StrategyType = LParam.StrategyType,
                    StrategyName = LParam.StrategyName
                };
                ChangeActiveOrders?.Invoke(this, ActiveOrdersE);

                ClosePosition();
                LState.Reset();
                Stop();
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print(String.Format("Ошибка при принудительной остановке стратегии: {0}", ex.Message), true);
                ChangeState?.Invoke(false, false, LParam.StrategyName);
            }
        }

        private void ClosePosition()
        {
            Print("Start closing!");
            if (LState.PosDirection == 0)
            {
                return;
            }

            if (LState.PosDirection == 1)
            {
                var tresult = CStrategyTrade.ExecuteByMarket(false, (double)LState.PurchasedAmount).Result;
                if (tresult != null)
                {
                    LState.PurchasedAmount -= tresult.BaseQty;
                    
                }
            }
            else
            {
                var tresult = CStrategyTrade.ExecuteByMarket(true, GetBuyAmount((double)LState.PurchasedAmount).Result).Result;
                if (tresult != null)
                {
                    LState.PurchasedAmount += tresult.BaseQty;
                }
            }
        }

        private async Task<double> GetBuyAmount(double AMarketCur, Ticker ticker = null)
        {
            double resAmount = 0;
            if (ticker == null)
            {
                ticker = await Param.Stock.GetMarketPrice(Param.Market, Print);
                if (ticker != null)
                {
                    resAmount = Math.Round(AMarketCur / (double)ticker.Ask, 8);
                }
                else
                {
                    Print("Ticker is null at buy!");
                }
            }
            else
            {
                resAmount = Math.Round(AMarketCur / (double)ticker.Ask, 8);
            }
            return resAmount;
        }
    }
}
