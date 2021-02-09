using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoTrade.BaseTypes;
using Newtonsoft.Json;

namespace CryptoTrade.Strategies
{
    public class InternalStrategyParamGrid : StrategyParam
    {
        public bool IsDirectionLong { get; set; }
        public ExTool.StepRepresent StepRepresentEnter { get; set; } = new ExTool.StepRepresent(1);
        public ExTool.StepRepresent StepRepresentClose { get; set; } = new ExTool.StepRepresent(1);
        public double GridFactor { get; set; }
        public int MaxStepsNumber { get; set; }
        public Dictionary<int, string> AdditionalTradings { get; set; } = new Dictionary<int, string>();    //step, params in json

        public InternalStrategyParamGrid() : base()
        {
            base.StrategyType = "Grid";
            Description.Add("IsDirectionLong", "Открывать позицию на Long (true/false)");
            Description.Add("StepRepresentEnter", "Шаг входа в позицию для сетки: [Шаг в %]-[Шаг в пунктах]-[Использовать шаг в %]");
            Description.Add("StepRepresentClose", "Шаг закрытия позиции для сетки: [Шаг в %]-[Шаг в пунктах]-[Использовать шаг в %]");
            Description.Add("GridFactor", "Множитель сетки");
            Description.Add("MaxStepsNumber", "Максимальное количество шагов");
            Description.Add("AdditionalTrading[step]", "Параметры дополнительной торговли для шага [step]");
        }

        public override void GetData(ref Dictionary<string, string> DataLParams)
        {
            base.GetData(ref DataLParams);
            DataLParams.Add("IsDirectionLong", IsDirectionLong.ToString());
            DataLParams.Add("StepRepresentEnter", StepRepresentEnter.StringRepresent());
            DataLParams.Add("StepRepresentClose", StepRepresentClose.StringRepresent());
            DataLParams.Add("GridFactor", GridFactor.ToString());
            DataLParams.Add("MaxStepsNumber", MaxStepsNumber.ToString());
            foreach (var item in AdditionalTradings)
            {
                DataLParams.Add("AdditionalTrading" + item.Key, item.Value);
            }
        }

        public override void LoadData(Dictionary<string, string> dict)
        {
            base.LoadData(dict);
            IsDirectionLong = Convert.ToBoolean(dict["IsDirectionLong"]);
            StepRepresentEnter = ExTool.StepRepresent.LoadFromString(dict["StepRepresentEnter"]);
            StepRepresentClose = ExTool.StepRepresent.LoadFromString(dict["StepRepresentClose"]);
            GridFactor = Convert.ToDouble(dict["GridFactor"].Replace(',', '.'), CultureInfo.InvariantCulture);
            MaxStepsNumber = Convert.ToInt32(dict["MaxStepsNumber"]);

            var ats = dict.Where(x => x.Key.Contains("AdditionalTrading"));
            AdditionalTradings.Clear();
            foreach (var item in ats)
            {
                int step = Convert.ToInt32(item.Key.Replace("AdditionalTrading", ""));
                AdditionalTradings.Add(step, item.Value);
            }
        }
    }

    public class InternalStrategyStateGrid : StrategyState
    {
        public int CurrentStep { get; set; }
        public double EnterPositionPrice { get; set; }
        public double EnterPositionSum { get; set; }
        public double ClosePositionPrice { get; set; }
        public double ClosePositionSum { get; set; }
        public string ZeroPoint => String.Format("{0}*{1}", Zsum1, Zsum2);
        public double Zsum1 = 0;
        public double Zsum2 = 0;

        public List<MiniLLStrategy> LLStrategies = new List<MiniLLStrategy>();  //for save, for CurrentStep
        public List<string> LLStrategiesStates = new List<string>();    //for load, in json, for CurrentStep

        public InternalStrategyStateGrid() : base()
        {
            Description.Add("CurrentStep", "Текущий шаг");
            Description.Add("EnterPositionPrice", "Цена входа в позицию");
            Description.Add("EnterPositionSum", "Сума на вход в позицию");
            Description.Add("ClosePositionPrice", "Цена закрытия позиции");
            Description.Add("ClosePositionSum", "Сума на выход из позиции");
            Description.Add("ZeroPoint", "Точка нуля [sum1]*[sum2]");
            Description.Add("LLStrategies", "Состояния дополнительной торговли на текущем шаге");
        }

        public override void GetData(ref Dictionary<string, string> DataLParams)
        {
            base.GetData(ref DataLParams);
            DataLParams.Add("CurrentStep", CurrentStep.ToString());
            DataLParams.Add("EnterPositionPrice", EnterPositionPrice.ToString());
            DataLParams.Add("EnterPositionSum", EnterPositionSum.ToString());
            DataLParams.Add("ClosePositionPrice", ClosePositionPrice.ToString());
            DataLParams.Add("ClosePositionSum", ClosePositionSum.ToString());
            DataLParams.Add("ZeroPoint", ZeroPoint);

            var svalues = new List<string>();
            foreach (var item in LLStrategies)
            {
                svalues.Add(item.GetState());
            }
            if (svalues.Count > 0)
            {
                DataLParams.Add("LLStrategies", JsonConvert.SerializeObject(svalues));
            }
        }

        public override void LoadData(Dictionary<string, string> dict)
        {
            base.LoadData(dict);
            CurrentStep = Convert.ToInt32(dict["CurrentStep"]);
            EnterPositionPrice = Convert.ToDouble(dict["EnterPositionPrice"].Replace(',', '.'), CultureInfo.InvariantCulture);
            EnterPositionSum = Convert.ToDouble(dict["EnterPositionSum"].Replace(',', '.'), CultureInfo.InvariantCulture);
            ClosePositionPrice = Convert.ToDouble(dict["ClosePositionPrice"].Replace(',', '.'), CultureInfo.InvariantCulture);
            ClosePositionSum = Convert.ToDouble(dict["ClosePositionSum"].Replace(',', '.'), CultureInfo.InvariantCulture);

            string[] tzstr = dict["ZeroPoint"].Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
            Zsum1 = Convert.ToDouble(tzstr[0].Replace(',', '.'), CultureInfo.InvariantCulture);
            Zsum2 = Convert.ToDouble(tzstr[1].Replace(',', '.'), CultureInfo.InvariantCulture);

            if (dict.ContainsKey("LLStrategies"))
            {
                LLStrategiesStates = JsonConvert.DeserializeObject<List<string>>(dict["LLStrategies"]);
            }
        }

        public override void Reset()
        {
            base.Reset();
            CurrentStep = 0;
            EnterPositionPrice = 0;
            EnterPositionSum = 0;
            ClosePositionPrice = 0;
            ClosePositionSum = 0;
            Zsum1 = 0;
            Zsum2 = 0;

            LLStrategies.Clear();
            LLStrategiesStates.Clear();
        }
    }

    public class StrategyGrid : Strategy
    {
        public sealed override event Action<bool, bool, string> ChangeState; //параметры: bool FromRun(or Stop), bool Result (good or bad), StrategyName
        public sealed override event EventHandler<ActiveOrdersGridEventArgs> ChangeActiveOrders;

        public override IStrategyParam Param => LParam;
        public override IStrategyState State => LState;

        public InternalStrategyParamGrid LParam { get; }
        public InternalStrategyStateGrid LState { get; }
        private readonly object LockLTicker = new object();
        private Ticker LastTicker = null;
        private int ErrorsCount = 0;

        public StrategyGrid(string uniqueID) : base(uniqueID)
        {
            LParam = new InternalStrategyParamGrid();
            LState = new InternalStrategyStateGrid();

            CStrategyTrade = new StrategyTrade(this, Print);
            CStrategyPrices = new StrategyPrices(this, Print)
            {
                OnUpdateTicker = OnUpdateTicker
            };
        }

        private void OnUpdateTicker(Ticker ticker)
        {
            if (LParam.MaxStepsNumber <= 0)
            {
                return;
            }
            lock (LockLTicker)
            {
                LastTicker = ticker;
            }

            if(ErrorsCount > 10)
            {
                Stop();
                return;
            }

            foreach (var item in LState.LLStrategies)
            {
                item.OnUpdateTicker(ticker);
            }

            if (LState.CurrentStep == 0)
            {
                var tresult = CStrategyTrade.ExecuteByMarket(LParam.IsDirectionLong, -1).Result;
                if (tresult != null)
                {
                    LState.CurrentStep = 1;
                    double avgPrice = (double)tresult.AveragePrice;
                    double NRsum = SetUpLLStrategies(avgPrice);

                    if (LParam.IsDirectionLong)
                    {
                        ZeroPointChange(avgPrice, (double)tresult.BaseQty);
                        LState.PurchasedAmount += tresult.BaseQty;

                        LState.EnterPositionPrice = DecreasePrice(avgPrice, LParam.StepRepresentEnter, LState.CurrentStep);
                        LState.EnterPositionSum = GetEnterPositionSum(tresult, LState.CurrentStep); //for buy

                        LState.ClosePositionPrice = IncreasePrice(avgPrice, LParam.StepRepresentClose, LState.CurrentStep);
                        LState.ClosePositionSum = NRsum + (double)tresult.BaseQty;//for sell
                    }
                    else
                    {
                        ZeroPointChange(avgPrice, (double)tresult.BaseQty);
                        LState.PurchasedAmount -= tresult.BaseQty;

                        LState.EnterPositionPrice = IncreasePrice(avgPrice, LParam.StepRepresentEnter, LState.CurrentStep);
                        LState.EnterPositionSum = GetEnterPositionSum(tresult, LState.CurrentStep);//for sell

                        LState.ClosePositionPrice = DecreasePrice(avgPrice, LParam.StepRepresentClose, LState.CurrentStep);//for buy
                        LState.ClosePositionSum = NRsum + (double)tresult.MarketQty;//Math.Round((double)tresult.MarketQty / LState.ClosePositionPrice, 8);
                    }
                    ActiveOrdersInfo();
                    Print(String.Format("Выполнено вход в позицию {0} на суму: {1} {2} по цене: {3}. ({4} {5})",
                       LParam.IsDirectionLong ? "Long" : "Short", tresult.BaseQty, LParam.Market.BaseCurrency, avgPrice,
                       tresult.MarketQty, LParam.Market.MarketCurrency));
                    SaveData(false);
                    ErrorsCount = 0;
                }
                else
                {
                    ErrorsCount++;
                }
                return;
            }

            if (LParam.IsDirectionLong)
            {
                if ((double)ticker.Bid > LState.ClosePositionPrice)
                {
                    var tresult = CStrategyTrade.ExecuteByMarket(false, LState.ClosePositionSum).Result;
                    if (tresult != null)
                    {
                        LState.PurchasedAmount -= tresult.BaseQty;
                        Print(String.Format("Позиция Long закрылась на шаге {0}. Продано: {1} {2}, цена: {3}. ({4} {5})",
                            LState.CurrentStep, tresult.BaseQty, LParam.Market.BaseCurrency, tresult.AveragePrice,
                            tresult.MarketQty, LParam.Market.MarketCurrency));
                        LState.CurrentStep = 0;
                        ClearState();
                        ActiveOrdersInfo();
                        SaveData(false);
                        ErrorsCount = 0;
                    }
                    else
                    {
                        ErrorsCount++;
                    }
                    return;
                }

                if (LState.CurrentStep < LParam.MaxStepsNumber && (double)ticker.Ask < LState.EnterPositionPrice)
                {
                    var tresult = CStrategyTrade.ExecuteByMarket(true, GetBuyAmount(LState.EnterPositionSum, ticker).Result).Result;
                    if (tresult != null)
                    {
                        LState.CurrentStep += 1;
                        double avgPrice = (double)tresult.AveragePrice;
                        double NRsum = SetUpLLStrategies(avgPrice);

                        LState.PurchasedAmount += (decimal)NRsum + tresult.BaseQty;
                        ZeroPointChange(avgPrice, (double)tresult.BaseQty);
                        LState.EnterPositionPrice = DecreasePrice(avgPrice, LParam.StepRepresentEnter, LState.CurrentStep);
                        LState.EnterPositionSum = GetEnterPositionSum(tresult, LState.CurrentStep); //for buy

                        double ZeroPoint = GetZeroPoint();
                        LState.ClosePositionPrice = IncreasePrice(ZeroPoint, LParam.StepRepresentClose, LState.CurrentStep);
                        LState.ClosePositionSum += NRsum + (double)tresult.BaseQty; //for sell
                        Print(String.Format("Выполнен переход на следующий шаг {0}. Куплено на: {1} {2}, цена: {3}. ({4} {5})",
                            LState.CurrentStep, tresult.MarketQty, LParam.Market.MarketCurrency, avgPrice,
                            tresult.BaseQty, LParam.Market.BaseCurrency));
                        ActiveOrdersInfo();
                        SaveData(false);
                        ErrorsCount = 0;
                    }
                    else
                    {
                        ErrorsCount++;
                    }
                    return;
                }
            }
            else
            {
                if ((double)ticker.Ask < LState.ClosePositionPrice)
                {
                    var tresult = CStrategyTrade.ExecuteByMarket(true, GetBuyAmount(LState.ClosePositionSum, ticker).Result).Result;
                    if (tresult != null)
                    {
                        LState.PurchasedAmount += tresult.BaseQty;
                        Print(String.Format("Позиция Short закрылась на шаге {0}. Куплено на: {1} {2}, цена {3}. ({4} {5})",
                            LState.CurrentStep, tresult.MarketQty, LParam.Market.MarketCurrency, tresult.AveragePrice,
                            tresult.BaseQty, LParam.Market.BaseCurrency));
                        LState.CurrentStep = 0;
                        ClearState();
                        ActiveOrdersInfo();
                        SaveData(false);
                        ErrorsCount = 0;
                    }
                    else
                    {
                        ErrorsCount++;
                    }
                    return;
                }

                if (LState.CurrentStep < LParam.MaxStepsNumber && (double)ticker.Bid > LState.EnterPositionPrice)
                {
                    var tresult = CStrategyTrade.ExecuteByMarket(false, LState.EnterPositionSum).Result;
                    if (tresult != null)
                    {
                        LState.CurrentStep += 1;
                        double avgPrice = (double)tresult.AveragePrice;
                        double NRsum = SetUpLLStrategies(avgPrice);

                        LState.PurchasedAmount -= tresult.BaseQty;
                        ZeroPointChange(avgPrice, (double)tresult.BaseQty);
                        LState.EnterPositionPrice = IncreasePrice(avgPrice, LParam.StepRepresentEnter, LState.CurrentStep);
                        LState.EnterPositionSum = GetEnterPositionSum(tresult, LState.CurrentStep);//for sell

                        double ZeroPoint = GetZeroPoint();
                        LState.ClosePositionPrice = DecreasePrice(ZeroPoint, LParam.StepRepresentClose, LState.CurrentStep);//for buy
                        LState.ClosePositionSum += NRsum + (double)tresult.MarketQty; //Math.Round((double)tresult.MarketQty / LState.ClosePositionPrice, 8);
                        Print(String.Format("Выполнен переход на следующий шаг {0}. Продано: {1} {2}, цена: {3}. ({4} {5})",
                            LState.CurrentStep, tresult.BaseQty, LParam.Market.BaseCurrency, avgPrice,
                            tresult.MarketQty, LParam.Market.MarketCurrency));
                        ActiveOrdersInfo();
                        SaveData(false);
                        ErrorsCount = 0;
                    }
                    else
                    {
                        ErrorsCount++;
                    }
                    return;
                }
            }
        }

        public override void Start(bool AtFirst)
        {
            if (AtFirst)
            {
                LState.Reset();
            }
            else
            {
                ActiveOrdersInfo();
            }
            ErrorsCount = 0;
            InitLLStrategies();

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

        private void InitLLStrategies(double basePrice = 0)
        {
            try
            {
                LState.LLStrategies.Clear();
                if (!LParam.AdditionalTradings.ContainsKey(LState.CurrentStep))
                {
                    return;
                }
                var CurStepLLParams = JsonConvert.DeserializeObject<List<string>>(LParam.AdditionalTradings[LState.CurrentStep]);

                for (int i = 0; i < CurStepLLParams.Count; i++)
                {
                    var nLLStrategy = new MiniLLStrategy(LParam.IsDirectionLong, CStrategyTrade, () => SaveData(false), Print, Stop);
                    nLLStrategy.LoadParams(CurStepLLParams[i]);
                    if (LState.LLStrategiesStates.Count > i)
                    {
                        nLLStrategy.LoadState(LState.LLStrategiesStates[i]);
                    }
                    else
                    {
                        if (basePrice == 0 && LState.LLStrategies.Count > 0)
                        {
                            nLLStrategy.FirstInit(LState.LLStrategies[0].BasePrice);
                        }
                        else
                        {
                            nLLStrategy.FirstInit(basePrice);
                        }
                    }
                    LState.LLStrategies.Add(nLLStrategy);
                }
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Error in InitLLStrategies: " + ex.Message);
            }
        }

        private double GetLLSum()
        {
            double sumPositions = 0;
            foreach (var item in LState.LLStrategies)
            {
                sumPositions += item.ClosePositionSum;
            }
            return sumPositions;
        }

        //return sum of unclosed positions
        private double SetUpLLStrategies(double basePrice)
        {
            double sumPositions = 0;
            try
            {
                sumPositions = GetLLSum();
                LState.LLStrategies.Clear();
                LState.LLStrategiesStates.Clear();
                InitLLStrategies(basePrice);
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Error in SetUpLLStrategies: " + ex.Message);
            }
            return sumPositions;
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
                switch (t.Key)
                {
                    case "ZeroPoint":
                        sb.AppendFormat("{0}: {1}\r\n", t.Key, GetZeroPoint());
                        break;
                    case "PurchasedAmount":
                        sb.AppendFormat("{0}: {1} {2}\r\n", t.Key, t.Value, LParam.Market.BaseCurrency);
                        break;
                    case "EnterPositionSum":
                        sb.AppendFormat("{0}: {1} {2}\r\n", t.Key, t.Value, LParam.IsDirectionLong ? LParam.Market.MarketCurrency : LParam.Market.BaseCurrency);
                        break;
                    case "ClosePositionSum":
                        sb.AppendFormat("{0}: {1} {2}\r\n", t.Key, t.Value, LParam.IsDirectionLong ? LParam.Market.BaseCurrency : LParam.Market.MarketCurrency);
                        break;
                    default:
                        sb.AppendFormat("{0}: {1}\r\n", t.Key, t.Value);
                        break;
                }
            }
            for (int i = 0; i < LState.LLStrategies.Count; i++)
            {
                sb.AppendFormat("Дополнительная торговля {0}:\r\n", (i + 1).ToString());
                sb.AppendFormat("EnterPrice: {0}, ClosePrice: {1}, ClosePositionSum: {2}\r\n",
                    LState.LLStrategies[i].EnterPrice, LState.LLStrategies[i].ClosePrice, LState.LLStrategies[i].ClosePositionSum);
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

        //нужная сума для перехода с шага FromStep на FromStep+1 (FromStep >= 1)
        private double GetEnterPositionSum(TradeResult tresult, int FromStep)
        {
            var CurRest = LParam.IsDirectionLong ? LParam.BalanceRestBuy : LParam.BalanceRestSell;
            int count = CurRest.Values.Count;
            double result = 0;
            if (FromStep > count - 1) //не хватает значений
            {
                if (LParam.IsDirectionLong) //for buy
                {
                    result = Math.Round((double)tresult.MarketQty * LParam.GridFactor, 8); //в MarketCurrency
                }
                else
                {
                    result = Math.Round((double)tresult.BaseQty * LParam.GridFactor, 8); //в BaseCurrency
                }
            }
            else
            {
                if (CurRest.IsPercentSize)
                {
                    result = CStrategyTrade.CalcSumFromPercent(CurRest.Values[FromStep], LParam.IsDirectionLong).Result;
                }
                else
                {
                    result = CurRest.Values[FromStep];
                }
            }
            return result;
        }

        //уменьшает цену для перехода на следующий шаг с теущего ForStep, ForStep from 1
        private double DecreasePrice(double BeginPrice, ExTool.StepRepresent stepRepresent, int ForStep)
        {
            double result = BeginPrice;
            int count = stepRepresent.Values.Count;
            double CurValue = ForStep > count ? stepRepresent.Values[count - 1] : stepRepresent.Values[ForStep - 1];
            if (stepRepresent.IsPercentSize)
            {
                result -= Math.Round((BeginPrice * CurValue) / 100d, 8);
            }
            else
            {
                result -= CurValue;
            }
            return result;
        }

        //увеличивает цену для перехода на следующий шаг с теущего ForStep, ForStep from 1
        private double IncreasePrice(double BeginPrice, ExTool.StepRepresent stepRepresent, int ForStep)
        {
            double result = BeginPrice;
            int count = stepRepresent.Values.Count;
            double CurValue = ForStep > count ? stepRepresent.Values[count - 1] : stepRepresent.Values[ForStep - 1];
            if (stepRepresent.IsPercentSize)
            {
                result += Math.Round((BeginPrice * CurValue) / 100d, 8);
            }
            else
            {
                result += CurValue;
            }
            return result;
        }

        private double GetZeroPoint()
        {
            if (LState.Zsum2 == 0)
            {
                return 0;
            }
            else
            {
                return Math.Round(LState.Zsum1 / LState.Zsum2, 8);
            }
        }

        private void ZeroPointChange(double LastPrice, double LastSum)
        {
            LState.Zsum1 += LastSum * LastPrice;
            LState.Zsum2 += LastSum;
        }

        private void ClearState()
        {
            LState.Zsum1 = 0;
            LState.Zsum2 = 0;

            LState.EnterPositionPrice = 0;
            LState.EnterPositionSum = 0;
            LState.ClosePositionPrice = 0;
            LState.ClosePositionSum = 0;
            LState.LLStrategies.Clear();
            LState.LLStrategiesStates.Clear();
        }

        public override void ActiveOrdersInfo()
        {
            var ActiveOrdersE = new ActiveOrdersGridEventArgs()
            {
                StrategyType = LParam.StrategyType,
                StrategyName = LParam.StrategyName
            };
            if (LState.CurrentStep > 0)
            {
                if (LState.CurrentStep < LParam.MaxStepsNumber)
                {
                    ActiveOrdersE.ActiveOrdersList.Add(new ActiveOrders()       //Enter Position
                    {
                        OrderType = "Limit",
                        Direction = LParam.IsDirectionLong ? "Buy" : "Sell",
                        Amount = LState.EnterPositionSum,
                        Price = LState.EnterPositionPrice,
                        Comment = String.Format("Переход на шаг {0}", LState.CurrentStep + 1)
                    });
                }

                ActiveOrdersE.ActiveOrdersList.Add(new ActiveOrders()       //Close Position
                {
                    OrderType = "Limit",
                    Direction = LParam.IsDirectionLong ? "Sell" : "Buy",
                    Amount = LState.ClosePositionSum,
                    Price = LState.ClosePositionPrice,
                    Comment = String.Format("Закрытие шага {0}", LState.CurrentStep)
                });
            }
            ChangeActiveOrders?.Invoke(this, ActiveOrdersE);
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
                SaveData();
                ChangeState?.Invoke(false, true, LParam.StrategyName);
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
            if (LState.ClosePositionSum <= 0)
            {
                return;
            }

            if (LParam.IsDirectionLong)
            {
                var tresult = CStrategyTrade.ExecuteByMarket(false, LState.ClosePositionSum).Result;
                if (tresult != null)
                {
                    LState.PurchasedAmount -= tresult.BaseQty;
                    Print(String.Format("Позиция Long закрылась на шаге {0}. Сума: {1} {2}, цена: {3}.",
                        LState.CurrentStep, tresult.BaseQty, LParam.Market.BaseCurrency, tresult.AveragePrice));
                    LState.CurrentStep = 0;
                    ClearState();
                    Stop();
                }
            }
            else
            {
                var tresult = CStrategyTrade.ExecuteByMarket(true, GetBuyAmount(LState.ClosePositionSum).Result).Result;
                if (tresult != null)
                {
                    LState.PurchasedAmount += tresult.BaseQty;
                    Print(String.Format("Позиция Short закрылась на шаге {0}. Сума: {1} {2}, цена {3}.",
                        LState.CurrentStep, tresult.BaseQty, LParam.Market.BaseCurrency, tresult.AveragePrice));
                    LState.CurrentStep = 0;
                    ClearState();
                    Stop();
                }
            }
        }

        public override double GetPositionState()
        {
            double result = 0;
            double zeropoint = GetZeroPoint();

            lock (LockLTicker)
            {
                if (LastTicker != null && zeropoint != 0)
                {
                    double ClosePrice = 0;
                    if (LParam.IsDirectionLong)
                    {
                        ClosePrice = (double)LastTicker.Bid;
                        result = ((ClosePrice - zeropoint) / zeropoint) * 100d;
                    }
                    else
                    {
                        ClosePrice = (double)LastTicker.Ask;
                        result = ((zeropoint - ClosePrice) / zeropoint) * 100d;
                    }
                    result = Math.Round(result, 2);
                }
            }
            return result;
        }
    }

    public class MiniLLStrategy
    {
        //Params
        public ExTool.StepRepresent EnterDistance { get; set; } = new ExTool.StepRepresent(1);
        public ExTool.StepRepresent CloseDistance { get; set; } = new ExTool.StepRepresent(1);
        public ExTool.StepRepresent BalanceRest { get; set; } = new ExTool.StepRepresent(1);

        //State
        public double BasePrice { get; set; }
        public double ClosePositionSum { get; set; }

        //Internal variables
        public double EnterPrice;
        public double ClosePrice;
        public bool IsDirectionBuy;

        private readonly StrategyTrade CurStrategyTrade;
        private readonly InvokePrint Print;
        private Action GSave;
        private event Action OnStop;
        private int ErrorsCount = 0;

        public MiniLLStrategy(bool isDirectionBuy, StrategyTrade curStrategyTrade, Action gSave, InvokePrint print, Action onStop)
        {
            IsDirectionBuy = isDirectionBuy;
            CurStrategyTrade = curStrategyTrade;
            GSave = gSave;
            Print = print;
            OnStop = onStop;
        }

        //[EnterDistance*CloseDistance*BalanceRest]
        public void LoadParams(string strParams)
        {
            string[] tspl = strParams.Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
            EnterDistance = ExTool.StepRepresent.LoadFromString(tspl[0]);
            CloseDistance = ExTool.StepRepresent.LoadFromString(tspl[1]);
            BalanceRest = ExTool.StepRepresent.LoadFromString(tspl[2]);
            ErrorsCount = 0;
        }

        //[BasePrice*ClosePositionSum]
        public void LoadState(string strState)
        {
            string[] tspl = strState.Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
            BasePrice = Convert.ToDouble(tspl[0].Replace(',', '.'), CultureInfo.InvariantCulture);
            ClosePositionSum = Convert.ToDouble(tspl[1].Replace(',', '.'), CultureInfo.InvariantCulture);
            ErrorsCount = 0;

            FirstInit(BasePrice);
        }

        public string GetState()
        {
            string result = String.Format("{0}*{1}", BasePrice.ToString(), ClosePositionSum.ToString());
            return result;
        }

        public void FirstInit(double basePrice)
        {
            ErrorsCount = 0;
            BasePrice = basePrice;
            if (IsDirectionBuy)
            {
                EnterPrice = ExTool.DecreasePrice(BasePrice, EnterDistance);
                ClosePrice = ExTool.IncreasePrice(EnterPrice, CloseDistance);
            }
            else
            {
                EnterPrice = ExTool.IncreasePrice(BasePrice, EnterDistance);
                ClosePrice = ExTool.DecreasePrice(EnterPrice, CloseDistance);
            }
        }

        private async Task<double> GetBuyAmount(double AMarketCur, Ticker ticker = null)
        {
            double resAmount = 0;
            if (ticker == null)
            {
                ticker = await CurStrategyTrade.Param.Stock.GetMarketPrice(CurStrategyTrade.Param.Market, Print);
                if (ticker != null)
                {
                    ErrorsCount = 0;
                    resAmount = Math.Round(AMarketCur / (double)ticker.Ask, 8);
                }
                else
                {
                    ErrorsCount++;
                    Print("Ticker is null at buy!");
                }
            }
            else
            {
                resAmount = Math.Round(AMarketCur / (double)ticker.Ask, 8);
            }
            return resAmount;
        }

        public void OnUpdateTicker(Ticker ticker)
        {
            if(ErrorsCount > 10)
            {
                OnStop?.Invoke();
                return;
            }

            if (ClosePositionSum == 0 && IsDirectionBuy && (double)ticker.Ask < EnterPrice)   //enter Long
            {
                var tresult = CurStrategyTrade.ExecuteByMarket(true, CurStrategyTrade.GetAmount(IsDirectionBuy, BalanceRest).Result).Result;
                if (tresult != null)
                {
                    ClosePositionSum = (double)tresult.BaseQty;
                    double avgPrice = (double)tresult.AveragePrice;

                    Print(String.Format("AT: Выполнено вход в позицию Long на суму: {0} {1} по цене: {2}. ({3} {4})",
                      tresult.BaseQty, CurStrategyTrade.Param.Market.BaseCurrency, avgPrice,
                     tresult.MarketQty, CurStrategyTrade.Param.Market.MarketCurrency));
                    GSave();
                    ErrorsCount = 0;
                }
                else
                {
                    ErrorsCount++;
                }
            }

            if (ClosePositionSum == 0 && !IsDirectionBuy && (double)ticker.Bid > EnterPrice)  //enter Short
            {
                var tresult = CurStrategyTrade.ExecuteByMarket(false, CurStrategyTrade.GetAmount(IsDirectionBuy, BalanceRest).Result).Result;
                if (tresult != null)
                {
                    ClosePositionSum = (double)tresult.MarketQty;
                    double avgPrice = (double)tresult.AveragePrice;

                    Print(String.Format("AT: Выполнено вход в позицию Short на суму: {0} {1} по цене: {2}. ({3} {4})",
                      tresult.BaseQty, CurStrategyTrade.Param.Market.BaseCurrency, avgPrice,
                     tresult.MarketQty, CurStrategyTrade.Param.Market.MarketCurrency));
                    GSave();
                    ErrorsCount = 0;
                }
                else
                {
                    ErrorsCount++;
                }
            }

            if (ClosePositionSum > 0 && IsDirectionBuy && (double)ticker.Bid > ClosePrice)     //close Long
            {
                var tresult = CurStrategyTrade.ExecuteByMarket(false, ClosePositionSum).Result;
                if (tresult != null)
                {
                    ClosePositionSum = 0;
                    double avgPrice = (double)tresult.AveragePrice;

                    Print(String.Format("AT: Позиция Long закрылась на суму: {0} {1} по цене: {2}. ({3} {4})",
                      tresult.BaseQty, CurStrategyTrade.Param.Market.BaseCurrency, avgPrice,
                     tresult.MarketQty, CurStrategyTrade.Param.Market.MarketCurrency));
                    GSave();
                    ErrorsCount = 0;
                }
                else
                {
                    ErrorsCount++;
                }
            }

            if (ClosePositionSum > 0 && !IsDirectionBuy && (double)ticker.Ask < ClosePrice)    //close Short
            {
                var tresult = CurStrategyTrade.ExecuteByMarket(true, GetBuyAmount(ClosePositionSum, ticker).Result).Result;
                if (tresult != null)
                {
                    ClosePositionSum = 0;
                    double avgPrice = (double)tresult.AveragePrice;

                    Print(String.Format("AT: Позиция Short закрылась на суму: {0} {1} по цене: {2}. ({3} {4})",
                      tresult.BaseQty, CurStrategyTrade.Param.Market.BaseCurrency, avgPrice,
                     tresult.MarketQty, CurStrategyTrade.Param.Market.MarketCurrency));
                    GSave();
                    ErrorsCount = 0;
                }
                else
                {
                    ErrorsCount++;
                }
            }
        }
    }
}
