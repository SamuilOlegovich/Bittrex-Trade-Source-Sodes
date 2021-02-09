using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CryptoTrade
{
    public class ExTool
    {
        public interface IStopOrder
        {
            bool DirectionBuy { get; set; } //направление по котороу ордер сработает
            double Amount { get; set; }  //сума
            decimal StopPrice { get; set; }  //цена срабатывания стоп-ордера

            bool UpdatePrice(decimal LastPrice);
            string ShowInfo();
        }

        public class StopOrder : IStopOrder
        {
            private readonly string OType = "Simple";
            public bool DirectionBuy { get; set; } //направление по котороу ордер сработает
            public double Amount { get; set; }  //сума
            public decimal StopPrice { get; set; }  //цена срабатывания стоп-ордера

            public StopOrder(bool DirectionBuy, double Amount, decimal StopPrice)
            {
                this.DirectionBuy = DirectionBuy;
                this.Amount = Amount;
                this.StopPrice = StopPrice;
            }

            public virtual bool UpdatePrice(decimal LastPrice)
            {
                if (!DirectionBuy)
                {
                    if (LastPrice < StopPrice)
                    {
                        return true;
                    }
                }
                else
                {
                    if (LastPrice > StopPrice)
                    {
                        return true;
                    }
                }
                return false;
            }

            public virtual string ShowInfo()
            {
                return String.Format("Тип: {0}, сума: {1}, StopPrice: {2}",
                   OType + " " + (DirectionBuy ? "Buy" : "Sell"), Amount, StopPrice);
            }
        }

        public class TrailingStopOrder : StopOrder
        {
            private readonly string OType = "Trailing";
            public decimal BestPrice { get; private set; } //уровень цены за которым следовать

            public TrailingStopOrder(bool DirectionBuy, double Amount, decimal StopPrice, decimal MarketPrice) : base(DirectionBuy, Amount, StopPrice)
            {
                BestPrice = MarketPrice;
            }

            public override bool UpdatePrice(decimal LastPrice)
            {
                if (base.UpdatePrice(LastPrice))
                {
                    return true;
                }

                if (!DirectionBuy)
                {
                    if (LastPrice > BestPrice)
                    {
                        StopPrice += LastPrice - BestPrice;
                        BestPrice = LastPrice;
                    }
                }
                else
                {
                    if (LastPrice < BestPrice)
                    {
                        StopPrice -= BestPrice - LastPrice;
                        BestPrice = LastPrice;
                    }
                }
                return false;
            }
        }

        public class TrailingStopWaitOrder : TrailingStopOrder
        {
            private readonly string OType = "TrailingWait";
            public decimal WaitPrice { get; private set; }
            public bool Activate { get; private set; }

            public TrailingStopWaitOrder(bool DirectionBuy, double Amount, decimal StopPrice, decimal MarketPrice, decimal WaitPrice, bool Activate = false)
                : base(DirectionBuy, Amount, StopPrice, MarketPrice)
            {
                this.WaitPrice = WaitPrice;
                this.Activate = Activate;
            }

            public override bool UpdatePrice(decimal LastPrice)
            {
                if (Activate && base.UpdatePrice(LastPrice))
                {
                    return true;
                }

                if (Activate == false)
                {
                    if (!DirectionBuy)
                    {
                        if (LastPrice > WaitPrice)
                        {
                            Activate = true;
                        }
                    }
                    else
                    {
                        if (LastPrice < WaitPrice)
                        {
                            Activate = true;
                        }
                    }
                }
                return false;
            }

            public override string ShowInfo()
            {
                return base.ShowInfo() + String.Format(", WaitPrice: {0}, Activate: {1}", WaitPrice, Activate);
            }
        }

        public class ArrayValues
        {
            public List<double> Values = new List<double>();

            public ArrayValues(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    Values.Add(0);
                }
            }

            public double this[int index]
            {
                get => Values[index];

                set => Values[index] = value;
            }

            public string StringRepresent()
            {
                var sb = new StringBuilder();
                foreach (double item in Values)
                {
                    sb.AppendFormat("{0}*", item.ToString());
                }
                if (sb.Length > 0)
                {
                    sb.Remove(sb.Length - 1, 1);
                }
                return sb.ToString();
            }

            public void LoadFromString(string SRepresent)
            {
                string[] svals = SRepresent.Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);

                Values = new List<double>(svals.Length);
                for (int i = 0; i < svals.Length; i++)
                {
                    Values.Add(Convert.ToDouble(svals[i].Replace(',', '.'), CultureInfo.InvariantCulture));
                }
            }
        }

        public class StepRepresent
        {
            public List<double> Values = new List<double>();
            public bool IsPercentSize;

            public StepRepresent(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    Values.Add(0);
                }
            }

            public double this[int index]
            {
                get => Values[index];

                set => Values[index] = value;
            }

            public string StringRepresent()
            {
                var sb = new StringBuilder();
                foreach (double item in Values)
                {
                    sb.AppendFormat("{0}*", item.ToString());
                }
                if (sb.Length > 0)
                {
                    sb.Remove(sb.Length - 1, 1);
                }
                sb.AppendFormat("@{0}", IsPercentSize ? "P" : "V");
                return sb.ToString();
            }

            public static StepRepresent LoadFromString(string SRepresent)
            {
                var result = new StepRepresent(1);
                result.TLoadFromString(SRepresent);
                return result;
            }

            private void TLoadFromString(string SRepresent)
            {
                string sPercValue = "";
                string[] tspl = SRepresent.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
                if (tspl.Length == 2)
                {
                    string[] svals = tspl[0].Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
                    Values = new List<double>(svals.Length);
                    for (int i = 0; i < svals.Length; i++)
                    {
                        Values.Add(Convert.ToDouble(svals[i].Replace(',', '.'), CultureInfo.InvariantCulture));
                    }
                    sPercValue = tspl[1].ToUpper();
                }
                else
                {
                    Values = new List<double>();
                    sPercValue = tspl[0].ToUpper();
                }

                if (sPercValue == "P")
                {
                    IsPercentSize = true;
                }
                else if (sPercValue == "V")
                {
                    IsPercentSize = false;
                }
                else
                {
                    IsPercentSize = Convert.ToBoolean(tspl[1]);
                }
            }
        }

        public static double IncreasePrice(double BeginPrice, StepRepresent stepRepresent)
        {
            double result = BeginPrice;
            double CurValue = stepRepresent.Values[0];
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

        public static double DecreasePrice(double BeginPrice, StepRepresent stepRepresent)
        {
            double result = BeginPrice;
            double CurValue = stepRepresent.Values[0];
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

        public static IStock StockByName(string StockName)
        {
            IStock resStock = null;
            switch (StockName)
            {
                case "Bittrex":
                    {
                        resStock = new BittrexStock();
                        break;
                    }
                case "Binance":
                    {
                        resStock = new BinanceStock();
                        break;
                    }
            }
            return resStock;
        }
    }
}
