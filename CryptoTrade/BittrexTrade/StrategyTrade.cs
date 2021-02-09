using CryptoTrade.BaseTypes;
using CryptoTrade.Strategies;
using System;
using System.Threading.Tasks;

namespace CryptoTrade
{
    public class StrategyTrade
    {
        private Strategy MainStrategy;
        public IStrategyParam Param => MainStrategy.Param;
        private IStock Stock => MainStrategy.Param.Stock;
        private IStrategyState State => MainStrategy.State;
        private readonly InvokePrint Print;

        public StrategyTrade(Strategy strategy, InvokePrint Print)
        {
            MainStrategy = strategy;
            this.Print = Print;
        }

        //Только для CrossMA
        public void ChangeStrategyState(Strategy CStrategy, InternalStrategyStateMA State, int NState, bool WithImplem, bool WithStop, bool FromSignal)
        {
            try
            {
                if (FromSignal == false) //вызвалось НЕ при срабатывании сигнала (с form1)
                {
                    if (WithImplem == true) //нужно выставлять ордер
                    {
                        if (WithStop == true) //выполнить не сейчас, а при сраб. сигнала
                        {
                            State.FuturePos = NState; //требуемая позиция
                            State.FutureStop = true; //флаг остановки 
                        }
                        else //выполнить сейчас
                        {
                            State.FutureStop = false; //отменить флаг остановки
                            ExposeOrder(State, NState);//ставим ордера
                        }
                    }
                    else //смена позиции без ордеров
                    {
                        ExposeOrder(State, NState); //false
                    }
                }
                else //вызвалось на сигнале
                {
                    if (State.FutureStop == true) //есть флаг остановки
                    {
                        ExposeOrder(State, State.FuturePos);//меняем на указанную
                        CStrategy.Stop(); //останавливаем стратегию
                    }
                    else
                    {
                        ExposeOrder(State, NState); //действия по сигналу
                    }
                }
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print(string.Format("Ошибка при изменении позиции: {0}", ex.Message), true);
            }
        }

        //Только для CrossMA
        /// <summary>
        /// Запланировать какие ордера выставлять в завис. от текущ. позиц. и указанного направления
        /// </summary>
        /// <param name="pdir"></param>
        private async void ExposeOrder(InternalStrategyStateMA State, int NewState)
        {
            if (NewState == 1)
            {
                Print("Сигнал на покупку.", true);
                if (State.PosDirection == 1)
                {
                    System.Media.SystemSounds.Beep.Play();
                    Print("С позиции лонг еще не вышли.");
                }
                else
                {
                    TradeResult tresult = await ExecuteByMarket(true);
                    decimal quantity1 = tresult.BaseQty;
                    if (quantity1 > 0)
                    {
                        State.PurchasedAmount += quantity1;
                    }
                    else
                    {
                        Print("Не удалось исполнить ордер на покупку.");
                    }
                    State.PosDirection = 1;
                }
            }

            if (NewState == -1)
            {
                Print("Сигнал на продажу.", true);
                if (State.PosDirection == -1)
                {
                    System.Media.SystemSounds.Beep.Play();
                    Print("С позиции шорт еще не вышли.");
                }
                else
                {
                    TradeResult tresult = await ExecuteByMarket(false);
                    decimal quantity1 = tresult.BaseQty;
                    if (quantity1 > 0)
                    {
                        State.PurchasedAmount -= quantity1;
                    }
                    else
                    {
                        Print("Не удалось исполнить ордер на продажу.");
                    }
                    State.PosDirection = -1;
                }
            }

            if (NewState == 0)
            {
                Print("Закрытие позиции.", true);
                if (State.PosDirection == 0)
                {
                    System.Media.SystemSounds.Beep.Play();
                    Print("Позиция не открыта.");
                }
                else
                {
                    if (State.PosDirection == 1)
                    {
                        TradeResult tresult = await ExecuteByMarket(false);
                        decimal quantity1 = tresult.BaseQty;
                        if (quantity1 > 0)
                        {
                            State.PurchasedAmount -= quantity1;
                        }
                        else
                        {
                            Print("Не удалось исполнить ордер на продажу.");
                        }
                    }
                    State.PosDirection = 0;
                }
            }
        }

        /// <summary>
        /// Сменить позицию на 1 шаг по рынку
        /// </summary>
        /// <param name="DirectionBuy"></param>
        /// <param name="Amount">Для sell в BaseCurrency, для buy в BaseCurrency</param>
        /// <returns></returns>
        public async Task<TradeResult> ExecuteByMarket(bool DirectionBuy, double Amount = -1)
        {
            TradeResult tresult = await ExecuteMarketOrder(DirectionBuy, Amount);
            if (tresult != null) //успешно
            {
                if (tresult.IsFilled)
                {
                    return tresult;
                }

                TradeResult TResult = await Stock.GetOrderAmount(tresult.OrderId, Print);
                if (TResult != null)
                {
                    return TResult;
                }
                else
                {
                    Print("Ошибка - информация про ордер не получена!", true);
                    return null;
                }
            }
            else
            {
                Print("Ошибка - ордер не исполнено!", true);
                return null;
            }
        }

        public async Task<double> CalcSumFromPercent(double PercentValue, bool DirectionBuy)
        {
            double Amount = 0;
            CurrencyBalance cbalance = null;
            if (DirectionBuy == true)
            {
                cbalance = await Stock.GetBalance(Param.Market.MarketCurrency, Print);
            }
            else
            {
                cbalance = await Stock.GetBalance(Param.Market.BaseCurrency, Print);
            }

            if (cbalance != null)
            {
                double available = (double)cbalance.Available;
                Amount = Math.Round((available * PercentValue) / 100d, 8); //уменьшить учитывая ограничение
            }
            else
            {
                Print("balance is null at buy (percent)!");
            }
            return Amount;
        }

        private async Task<TradeResult> ExecuteMarketOrder(bool DirectionBuy, double Amount = -1)
        {
            try
            {
                if (Amount < 0)
                {
                    Amount = await GetAmount(DirectionBuy, DirectionBuy ? Param.BalanceRestBuy : Param.BalanceRestSell);
                }

                return await Stock.ExecuteMarket(Param.Market, Amount, DirectionBuy, Print);
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print(string.Format("Ошибка выставления заявки: {0}", ex.Message), true);
                return null;
            }
        }

        //возвращает суму в BaseCurrency
        public async Task<double> GetAmount(bool DirectionBuy, ExTool.StepRepresent BalanceRest)
        {
            double amount = 0; //в BaseCurrency
            double available = 0;
            string Currency = "";
            if (DirectionBuy)
            {
                if (BalanceRest.IsPercentSize)
                {
                    Currency = Param.Market.MarketCurrency;
                    CurrencyBalance cbalance = await Stock.GetBalance(Currency, Print);
                    Ticker ticker = await Stock.GetMarketPrice(Param.Market, Print);
                    if (cbalance != null && ticker != null)
                    {
                        available = (double)cbalance.Available;
                        available = Math.Round((available * BalanceRest[0]) / 100d, 8); //уменьшить учитывая ограничение
                        amount = Math.Round(available / (double)ticker.Ask, 8);
                    }
                    else
                    {
                        Print("balance or ticker is null at buy!");
                    }
                }
                else
                {
                    Ticker ticker = await Stock.GetMarketPrice(Param.Market, Print);
                    if (ticker != null)
                    {
                        amount = Math.Round(BalanceRest[0] / (double)ticker.Ask, 8);
                    }
                    else
                    {
                        Print("ticker is null at buy!");
                    }
                }
            }
            else
            {
                if (Param.SellOnlyBought)
                {
                    amount = (double)State.PurchasedAmount;
                }
                else
                {
                    if (BalanceRest.IsPercentSize)
                    {
                        Currency = Param.Market.BaseCurrency;
                        CurrencyBalance cbalance = await Stock.GetBalance(Currency, Print);
                        if (cbalance != null)
                        {
                            available = (double)cbalance.Available;
                            amount = Math.Round((available * BalanceRest[0]) / 100d, 8);
                        }
                        else
                        {
                            Print("balance is null at sell!");
                        }
                    }
                    else
                    {
                        amount = BalanceRest[0];
                    }
                }
            }
            if (amount < 0.00000001)
            {
                throw new Exception(string.Format("Доступно: {0}, cума на ордер: {1}. Недостаточно {2} для заявки!",
                    available, amount, Currency));
            }
            return amount;
        }
    }
}
