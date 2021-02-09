using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CryptoTrade
{
    public class StrategyStopOrders
    {
        public event Action<string, decimal> PerformedStopOrder; //OrderName, Amount

        private readonly InvokePrint Print;
        private StrategyTrade CStrategyTrade;
        private StrategyPrices CStrategyPrices;

        private Timer timerStopOrders; //проверка и исполнение стоп-ордеров
        private bool LockCheckStopOrders; //блок на выполнение Stop ордера
        private Dictionary<string, ExTool.IStopOrder> StopOrders = new Dictionary<string, ExTool.IStopOrder>();

        public StrategyStopOrders(Strategy strategy)
        {
            Print = strategy.Print;
            CStrategyTrade = strategy.CStrategyTrade;
            CStrategyPrices = strategy.CStrategyPrices;
        }

        public async void AddTrailingStopOrder(string OrderName, bool DirectionStopBuy, double Distance)
        {
            decimal Rate = 0;
            decimal StopPrice = 0;
            try
            {
                var ticker = await CStrategyPrices.GetMarketPrice();
                if (DirectionStopBuy)
                {
                    Rate = ticker.Ask;
                    StopPrice = Rate + (decimal)Math.Round((double)Rate * Distance, 8);
                }
                else
                {
                    Rate = ticker.Bid;
                    StopPrice = Rate - (decimal)Math.Round((double)Rate * Distance, 8);
                }

                double Eamount = await CStrategyTrade.GetAmount(DirectionStopBuy,
                    DirectionStopBuy ? CStrategyTrade.Param.BalanceRestBuy : CStrategyTrade.Param.BalanceRestSell);
                var TrailingStopOrder = new ExTool.TrailingStopOrder(DirectionStopBuy, Eamount, StopPrice, Rate);
                StopOrders.Add(OrderName, TrailingStopOrder);
                if (timerStopOrders == null)
                {
                    timerStopOrders = new Timer(TimerCheckStopOrders);
                    timerStopOrders.Change(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(800));
                }
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print(String.Format("Ошибка выставления TrailingStop ордера: {0}", ex.Message), true);
            }
        }

        private async void TimerCheckStopOrders(object state)
        {
            if (LockCheckStopOrders)
            {
                return;
            }

            LockCheckStopOrders = true;

            foreach (var SOrder in StopOrders)
            {
                try
                {
                    var StopOrder = SOrder.Value;
                    var ticker = await CStrategyPrices.GetMarketPrice();
                    decimal price = StopOrder.DirectionBuy ? ticker.Ask : ticker.Bid;

                    if (StopOrder.UpdatePrice(price))
                    {
                        var TResult = await CStrategyTrade.ExecuteByMarket(StopOrder.DirectionBuy, StopOrder.Amount);
                        if (TResult != null)
                        {
                            Print(String.Format("Сработал {0} ордер на {1}. Cума: {2}\r\n",
                                SOrder.Key, StopOrder.DirectionBuy ? "покупку" : "продажу", TResult.BaseQty), true);
                            PerformedStopOrder?.Invoke(SOrder.Key, TResult.BaseQty);

                            StopOrders.Remove(SOrder.Key);
                            if (StopOrders.Count == 0)
                            {
                                timerStopOrders.Dispose();
                            }
                        }
                        else
                        {
                            Print(String.Format("Ошибка исполнения {0}.\r\n", SOrder.Key), true);
                            PerformedStopOrder?.Invoke(SOrder.Key, 0);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Print("In TimerCheckStopOrders: " + ex.Message);
                    PerformedStopOrder?.Invoke(SOrder.Key, 0);
                }
            }
            LockCheckStopOrders = false;
        }

        public void CancelStopOrder(string OrderName)
        {
            while (LockCheckStopOrders)
            {
                Thread.Sleep(100);
            }
            LockCheckStopOrders = true;

            if (StopOrders.ContainsKey(OrderName))
            {
                StopOrders.Remove(OrderName);
            }

            if (StopOrders.Count == 0)
            {
                if (timerStopOrders != null)
                {
                    timerStopOrders.Dispose();
                }
            }
            LockCheckStopOrders = false;
        }

        public void CancelAllStopOrders()
        {
            while (LockCheckStopOrders)
            {
                Thread.Sleep(100);
            }
            LockCheckStopOrders = true;

            StopOrders.Clear();
            if (timerStopOrders != null)
            {
                timerStopOrders.Dispose();
            }
            LockCheckStopOrders = false;
        }

        public string ShowInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Активные стоп-ордера:");
            foreach (var SOrder in StopOrders)
            {
                sb.AppendLine(SOrder.Value.ShowInfo());
            }

            return sb.ToString();
        }
    }
}
