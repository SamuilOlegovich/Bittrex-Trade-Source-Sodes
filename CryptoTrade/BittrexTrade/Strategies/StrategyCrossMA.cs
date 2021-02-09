using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Threading;
using CryptoTrade.BaseTypes;

namespace CryptoTrade.Strategies
{
    public class InternalStrategyParamMA : StrategyParam
    {
        public int ShortPeriod { get; set; }
        public int LongPeriod { get; set; }
        public double DiffPerc { get; set; }
        public bool SetTrailingStopOrder { get; set; }
        public double DistanceTrailingStopInLong { get; set; }
        public double DistanceTrailingStopInShort { get; set; }
        public bool OneByCandle { get; set; }

        public InternalStrategyParamMA() : base()
        {
            base.StrategyType = "CrossMA";
            Description.Add("ShortPeriod", "Период Short EMA");
            Description.Add("LongPeriod", "Период Long EMA");
            Description.Add("DiffPerc", "Необходимая разница между линиями EMA для сигнала в %");
            Description.Add("SetTrailingStopOrder", "Ставить ли TrailingStop");
            Description.Add("DistanceTrailingStopInLong", "Расстояние в % для TrailingStop продажи");
            Description.Add("DistanceTrailingStopInShort", "Расстояние в % для TrailingStop покупки");
            Description.Add("OneByCandle", "Одна сделка на свечу");
        }

        public override void GetData(ref Dictionary<string, string> DataLParams)
        {
            base.GetData(ref DataLParams);
            DataLParams.Add("ShortPeriod", ShortPeriod.ToString());
            DataLParams.Add("LongPeriod", LongPeriod.ToString());
            DataLParams.Add("DiffPerc", DiffPerc.ToString());
            DataLParams.Add("SetTraillingStopOrder", SetTrailingStopOrder.ToString());
            DataLParams.Add("DistanceTrailingStopInLong", DistanceTrailingStopInLong.ToString());
            DataLParams.Add("DistanceTrailingStopInShort", DistanceTrailingStopInShort.ToString());
            DataLParams.Add("OneByCandle", OneByCandle.ToString());
        }

        public override void LoadData(Dictionary<string, string> dict)
        {
            base.LoadData(dict);
            ShortPeriod = Convert.ToInt32(dict["ShortPeriod"]);
            LongPeriod = Convert.ToInt32(dict["LongPeriod"]);
            DiffPerc = Convert.ToDouble(dict["DiffPerc"].Replace(',', '.'), CultureInfo.InvariantCulture);
            SellOnlyBought = Convert.ToBoolean(dict["SellOnlyBought"]);
            SetTrailingStopOrder = Convert.ToBoolean(dict["SetTraillingStopOrder"]);
            DistanceTrailingStopInLong = Convert.ToDouble(dict["DistanceTrailingStopInLong"].Replace(',', '.'), CultureInfo.InvariantCulture);
            DistanceTrailingStopInShort = Convert.ToDouble(dict["DistanceTrailingStopInShort"].Replace(',', '.'), CultureInfo.InvariantCulture);
            OneByCandle = Convert.ToBoolean(dict["OneByCandle"]);
        }
    }

    public class InternalStrategyStateMA : StrategyState
    {
        public DateTime ExecuteCandleTime { get; set; }
        public double LastMALong { get; set; }
        public double LastMAShort { get; set; }
        public double PrevMALong { get; set; }
        public double PrevMAShort { get; set; }
        public int PrevCross { get; set; }
        public int PosDirection { get; set; }
        public int FuturePos { get; set; }
        public bool FutureStop { get; set; }

        public InternalStrategyStateMA() : base()
        {
            Description.Add("ExecuteCandleTime", "Время свечи где была посленяя сделка.");
            Description.Add("LastMALong", "Последнее значение MA Long");
            Description.Add("LastMAShort", "Последнее значение MA Short");
            Description.Add("PrevMALong", "Предыдущее значение MA Long");
            Description.Add("PrevMAShort", "Предыдущее значение MA Short");
            Description.Add("PrevCross", "Направление последнего пересечения: -1, 0, 1");
            Description.Add("PosDirection", "Направление позиции: -1 - Short, 0 - None, 1 - Long.");
            Description.Add("FuturePos", "Требуемая позиция на следующем сигнале: -1, 0, 1");
            Description.Add("FutureStop", "Делать остановку на следующем сигнале.");
        }

        public override void GetData(ref Dictionary<string, string> DataLParams)
        {
            base.GetData(ref DataLParams);
            DataLParams.Add("ExecuteCandleTime", ExecuteCandleTime.ToString());
            DataLParams.Add("LastMALong", LastMALong.ToString());
            DataLParams.Add("LastMAShort", LastMAShort.ToString());
            DataLParams.Add("PrevMALong", PrevMALong.ToString());
            DataLParams.Add("PrevMAShort", PrevMAShort.ToString());
            DataLParams.Add("PrevCross", PrevCross.ToString());
            DataLParams.Add("PosDirection", PosDirection.ToString());
            DataLParams.Add("FuturePos", FuturePos.ToString());
            DataLParams.Add("FutureStop", FutureStop.ToString());
        }

        public override void LoadData(Dictionary<string, string> dict)
        {
            base.LoadData(dict);
            ExecuteCandleTime = Convert.ToDateTime(dict["ExecuteCandleTime"]);
            LastMALong = Convert.ToDouble(dict["LastMALong"].Replace(',', '.'), CultureInfo.InvariantCulture);
            LastMAShort = Convert.ToDouble(dict["LastMAShort"].Replace(',', '.'), CultureInfo.InvariantCulture);
            PrevMALong = Convert.ToDouble(dict["PrevMALong"].Replace(',', '.'), CultureInfo.InvariantCulture);
            PrevMAShort = Convert.ToDouble(dict["PrevMAShort"].Replace(',', '.'), CultureInfo.InvariantCulture);
            PrevCross = Convert.ToInt32(dict["PrevCross"]);
            PosDirection = Convert.ToInt32(dict["PosDirection"]);
            FuturePos = Convert.ToInt32(dict["FuturePos"]);
            FutureStop = Convert.ToBoolean(dict["FutureStop"]);
        }

        public override void Reset()
        {
            base.Reset();
            ExecuteCandleTime = DateTime.MinValue;
            LastMALong = 0;
            LastMAShort = 0;
            PrevMALong = 0;
            PrevMAShort = 0;
            PrevCross = 0;
            PosDirection = 0;
            FuturePos = 0;
            FutureStop = false;
        }
    }

    public class StrategyCrossMA : Strategy
    {
        public sealed override event Action<bool, bool, string> ChangeState; //параметры: bool FromRun(or Stop), bool Result (good or bad), StrategyName

        private StrategyStopOrders CStrategyStopOrders;

        public override IStrategyParam Param
        {
            get
            {
                return (IStrategyParam)LParam;
            }
        }
        public override IStrategyState State
        {
            get
            {
                return (IStrategyState)LState;
            }
        }

        public InternalStrategyParamMA LParam { get; }
        public InternalStrategyStateMA LState { get; }

        private bool CurrentCandleExecute
        {
            get
            {
                return CStrategyPrices.LastCandleTime == LState.ExecuteCandleTime;
            }
        }

        public StrategyCrossMA(string uniqueID) : base(uniqueID)
        {
            LParam = new InternalStrategyParamMA();
            LState = new InternalStrategyStateMA();

            CStrategyTrade = new StrategyTrade(this, Print);
            CStrategyPrices = new StrategyPrices(this, Print);

            CStrategyStopOrders = new StrategyStopOrders(this);
            CStrategyStopOrders.PerformedStopOrder += OnPerformedStopOrder;

            CStrategyPrices.OnUpdateCandles = OnUpdateCandles;
        }

        private void OnUpdateCandles(List<Candle> Candles)
        {
            if (Candles.Count < LParam.ShortPeriod || Candles.Count < LParam.LongPeriod)
            {
                System.Media.SystemSounds.Beep.Play();
                Print(String.Format("Недостаточно свечей для нахождения МА!"), true);
                return;
            }

            if (LState.LastMALong != 0 && LState.LastMAShort != 0)
            {
                LState.PrevMALong = LState.LastMALong;
                LState.PrevMAShort = LState.LastMAShort;
            }

            decimal Sum = 0;
            for (int j = 0; j < LParam.ShortPeriod; j++)
            {
                Sum += Candles[(Candles.Count - 1) - j].Close;
            }
            LState.LastMAShort = Math.Round((double)Sum / LParam.ShortPeriod, 8);

            Sum = 0;
            for (int j = 0; j < LParam.LongPeriod; j++)
            {
                Sum += Candles[(Candles.Count - 1) - j].Close;
            }
            LState.LastMALong = Math.Round((double)Sum / LParam.LongPeriod, 8);
            Decide();
        }

        public override void Start(bool AtFirst)
        {
            //if (LState.IsStartegyRun == true)
            //{
            //    Print("Стратегия уже запущена!", true);
            //    return;
            //}

            if (AtFirst)
                LState.Reset();

            if (CStrategyPrices.StartUpdateCandles())
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

        /// <summary>
        /// Возвращает направление пересечения
        /// </summary>
        /// <returns></returns>
        private int IsCrosses()
        {
            int result = 0;
            if (LState.PrevMALong > LState.PrevMAShort && LState.LastMAShort > LState.LastMALong)
            {
                result = 1;
            }
            else
            {
                if (LState.PrevMALong < LState.PrevMAShort && LState.LastMAShort < LState.LastMALong)
                    result = -1;
            }
            return result;
        }

        private void Decide()
        {
            double pdiff = 0;
            int CurCross = 0;
            try
            {
                pdiff = 1 - Math.Min(LState.LastMALong, LState.LastMAShort) / Math.Max(LState.LastMALong, LState.LastMAShort);
                CurCross = IsCrosses();
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print(String.Format("Ошибка Decide: {0}", ex.Message), true);
                return;
            }
            if (pdiff >= LParam.DiffPerc)   //разница достаточна
            {
                if (LParam.OneByCandle == true)
                {
                    if (CurrentCandleExecute == true) //уже была сделка в текущей свече
                    {
                        if (CurCross != 0 || LState.PrevCross != 0)
                        {
                            Print("Уже была сделка на текущую свечу.");
                            if (CurCross != 0)
                                LState.PrevCross = CurCross;
                        }
                        return;
                    }
                }
                //Print("Текущая разница: " + Math.Round(pdiff, 3) + " достаточна");
                if (CurCross != 0) //пересеч. сейчас
                {
                    Print("Есть пересечение и разница достаточна - смена позиции.", true);
                    CStrategyTrade.ChangeStrategyState(this, LState, CurCross, true, false, true);//ExposeOrder(CurCross);
                    LState.ExecuteCandleTime = CStrategyPrices.LastCandleTime;
                    SetStopOrders();
                }
                else
                {
                    if (LState.PrevCross != 0) //было пересеч. раньше
                    {
                        Print("Разница достаточна, смена позиции по прошлому пересечению.", true);
                        CStrategyTrade.ChangeStrategyState(this, LState, LState.PrevCross, true, false, true);//ExposeOrder(PrevCross);
                        LState.ExecuteCandleTime = CStrategyPrices.LastCandleTime;
                        SetStopOrders();
                    }
                }
                LState.PrevCross = 0; //аннулировать пересеч. раньше
            }
            else
            {
                //Print("Текущая разница: " + Math.Round(pdiff, 3) + " не достаточна");
                if (CurCross != 0)
                {
                    Print("Разница не достаточна, запомнить текущее пересечение", true);
                    LState.PrevCross = CurCross; //запомнить пересечение
                }
            }
        }

        private void SetStopOrders()
        {
            if (LParam.SetTrailingStopOrder == false)
                return;
            if (LState.PosDirection == 0)
                return;

            CStrategyStopOrders.CancelAllStopOrders();
            bool DirectionStopBuy = LState.PosDirection == 1 ? false : true; //сделка стоп-ордера
            double Distance = DirectionStopBuy ? LParam.DistanceTrailingStopInShort : LParam.DistanceTrailingStopInLong;
            string OrderName = DirectionStopBuy ? "TrailingStopOrderInShort" : "TrailingStopOrderInLong";
            CStrategyStopOrders.AddTrailingStopOrder(OrderName, DirectionStopBuy, Distance);
        }

        private void OnPerformedStopOrder(string OrderName, decimal Amount)
        {
            if (Amount == 0)
                return;

            if (OrderName == "TrailingStopOrderInShort")
            {
                LState.PosDirection = 1;
                LState.PurchasedAmount += Amount;
            }
            if (OrderName == "TrailingStopOrderInLong")
            {
                LState.PosDirection = -1;
                LState.PurchasedAmount -= Amount;
            }
            SetStopOrders();
        }

        public override void Stop()
        {
            try
            {
                CStrategyPrices.StopUpdates();
                CStrategyStopOrders.CancelAllStopOrders();

                LState.IsStartegyRun = false;
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

        public override string ShowInfo(bool NotOnlyParams = true)
        {
            string bstring = base.ShowInfo();
            bstring += CStrategyStopOrders.ShowInfo();
            return bstring;
        }
    }
}
