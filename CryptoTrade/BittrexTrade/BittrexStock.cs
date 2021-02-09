using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BittrexSharp;
using BittrexSharp.Domain;
using System.Threading;
using System.Globalization;

namespace CryptoTrade
{
    public class BittrexStock : IStock
    {
        static private Bittrex bittrex;

        public BittrexStock()
        {
            if (bittrex == null)
                bittrex = new Bittrex();
        }

        public static void SetApiKeys(string apiKey, string apiSecret)
        {
            bittrex = new Bittrex(apiKey, apiSecret);
        }

        public string GetStockName()
        {
            return "Bittrex";
        }

        public void RestartWebSocket()
        {

        }

        public async Task<List<BaseTypes.Market>> GetMarkets(InvokePrint Print)
        {
            try
            {
                var tmp = await bittrex.GetMarkets();
                List<BittrexSharp.Domain.Market> MarketsBitttrex = tmp.ToList();
                MarketsBitttrex.RemoveAll((x) => x.IsActive == false);

                List<BaseTypes.Market> resMarkets = new List<BaseTypes.Market>();
                foreach (var t in MarketsBitttrex)
                    resMarkets.Add(new BaseTypes.Market
                    {
                        MarketName = t.MarketName,
                        BaseCurrency = t.BaseCurrency,
                        MarketCurrency = t.MarketCurrency
                    });
                return resMarkets;
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Ошибка BittrexApi GetMarkets: " + ex.Message);
                return null;
            }
        }

        public async Task<List<BaseTypes.CurrencyBalance>> GetAllBalances(InvokePrint Print)
        {
            try
            {
                var bal = await bittrex.GetBalances();
                List<BaseTypes.CurrencyBalance> balances = new List<BaseTypes.CurrencyBalance>();
                foreach (var t in bal)
                    balances.Add(new BaseTypes.CurrencyBalance
                    {
                        Currency = t.Currency,
                        Balance = t.Balance,
                        Available = t.Available
                    });
                return balances;
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Ошибка BittrexApi GetAllBalances: " + ex.Message);
                return null;
            }
        }

        public async Task<BaseTypes.CurrencyBalance> GetBalance(string Currency, InvokePrint Print)
        {
            try
            {
                var cbal = await bittrex.GetBalance(Currency);
                BaseTypes.CurrencyBalance balance = new BaseTypes.CurrencyBalance
                {
                    Currency = cbal.Currency,
                    Balance = cbal.Balance,
                    Available = cbal.Available
                };
                return balance;
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print(String.Format("Ошибка BittrexApi GetBalance: {0}\r\nCurrency: {1}.", ex.Message, Currency));
                return null;
            }
        }

        public async Task<BaseTypes.TradeResult> ExecuteMarket(BaseTypes.Market Market, double Amount, bool DirectionBuy, InvokePrint Print)
        {
            decimal rate = 0;
            decimal amount = 0;
            try
            {
                BaseTypes.Ticker ticker = await GetMarketPrice(Market, Print);
                if (DirectionBuy)
                {
                    rate = (decimal)ticker.Ask;
                    if (rate < 0.00001M)
                        rate += (decimal)((double)rate * 0.1);
                    else
                        rate += (decimal)((double)rate * 0.01);
                }
                else
                {
                    rate = (decimal)ticker.Bid;
                    rate -= (decimal)((double)rate * 0.25);
                }
                rate = Math.Round(rate, 8);
                
                if (DirectionBuy)
                {
                    amount = (decimal)Amount;
                }
                else
                {
                    amount = Math.Round((decimal)Amount * (decimal)ticker.Bid, 8); 
                }
                string uuid = await ExecuteOrder(Market, amount, rate, DirectionBuy, Print);

                BaseTypes.TradeResult tresult = new BaseTypes.TradeResult()
                {
                    Symbol = Market.MarketName,
                    OrderId = uuid,

                    IsFilled = false
                };
                return tresult;
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print(String.Format("Ошибка BittrexApi ExecuteMarket: {0}.\r\nMarket: {1}, Amount: {2}, DirectionBuy: {3}.\r\nrate: {4}, amount: {5}.",
                    ex.Message, Market.ToString(), Amount, DirectionBuy, rate, amount));
                return null;
            }
        }

        public async Task<BaseTypes.Ticker> GetMarketPrice(BaseTypes.Market Market, InvokePrint Print)
        {
            try
            {
                Ticker tmp = await bittrex.GetTicker(Market.MarketName);
                if (tmp.Ask == null || tmp.Bid == null)
                {
                    Print("Ask or Bid is null!");
                    return null;
                }
                return new BaseTypes.Ticker
                {
                    Ask = tmp.Ask.Value,
                    Bid = tmp.Bid.Value,
                };
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print(String.Format("Ошибка BitrexApi GetMarketPrice: {0}.\r\nMarket: {1}.",
                    ex.Message, Market.ToString()));
                return null;
            }
        }

        public async Task<List<BaseTypes.Candle>> GetCandles(BaseTypes.Market Market, Candle_Interval Interval, int Limit, InvokePrint Print)
        {
            try
            {
                var tmp = await bittrex.GetCandles(Market.MarketName, Interval.ToString());
                List<Candle> BCandles = tmp.ToList();
                List<BaseTypes.Candle> lastByHistory = await CandleByMarketHistory(Market, Interval, Print);

                List<BaseTypes.Candle> Candles = new List<BaseTypes.Candle>(BCandles.Count);
                foreach (var t in BCandles)
                    Candles.Add(new BaseTypes.Candle
                    {
                        Open = t.Open,
                        Hight = t.Hight,
                        Low = t.Low,
                        Close = t.Close,
                        Time = t.Time
                    });

                if (lastByHistory != null)
                {
                    Candles.AddRange(lastByHistory);
                }
                return Candles;
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print(String.Format("Ошибка BittrexApi GetCandles: {0}.\r\nMarket: {1}, Interval: {2}",
                    ex.Message, Market.ToString(), Interval.ToString()));
                return null;
            }
        }

        public async Task<BaseTypes.TradeResult> GetOrderAmount(string OrderID, InvokePrint Print)
        {
            try
            {
                string[] arrtmp = OrderID.Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
                BaseTypes.Market market = BaseTypes.Market.LoadFromString(arrtmp[0]);
                string Uuid = arrtmp[1];

                decimal amount = -1;
                int MaxRepeat = 10;
                int CurRepeat = 0;
                BaseTypes.TradeResult tresult = new BaseTypes.TradeResult();
                while (amount == -1 && CurRepeat < MaxRepeat)
                {
                    try
                    {
                        List<HistoricOrder> OrderHistory = await GetOrderHistory(market, Print);
                        HistoricOrder needOrder = OrderHistory.Find(x => x.OrderUuid == Uuid);
                        if (needOrder != null)
                        {
                            tresult.BaseQty = needOrder.Quantity;
                            tresult.AveragePrice = needOrder.Price;
                            tresult.IsFilled = true;
                            amount = 2;
                        }
                        else
                            amount = 0;
                    }
                    catch
                    {
                        CurRepeat++;
                        Thread.Sleep(500); //что бы обновилась история ордеров на сервере
                    }
                }
                if (amount == -1)
                    throw new Exception("Не удалось получить информацию про ордер!");
                if (amount == 0)
                    throw new Exception("Не удалось получить информацию про ордер, ордер не найдено!");
                return tresult;
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Ошибка BittrexApi OrderAmount: " + ex.Message);
                return null;
            }
        }

        //private void ShowOrderInfo(HistoricOrder order, StringBuilder sb = null)
        //{
        //    if (sb == null)
        //        sb = new StringBuilder();
        //    string direct = order.OrderType;
        //    sb.AppendFormat("{0} {1} {2} по цене {3}. {4} {5} {6}. Время: {7}\r\n",
        //       direct, order.Quantity, Param.Market.MarketCurrency, order.PricePerUnit,
        //       direct.Contains("BUY") ? "Потрачено" : "Получено", order.Price, Param.Market.BaseCurrency, order.Timestamp);
        //    Print(sb.ToString(), true);
        //}

        private async Task<decimal> GetMinTradeSize(Market Market, InvokePrint Print)
        {
            try
            {
                var tmp = await bittrex.GetMarkets();
                List<BittrexSharp.Domain.Market> MarketsBitttrex = tmp.ToList();
                MarketsBitttrex.RemoveAll((x) => x.IsActive == false);

                foreach (var t in MarketsBitttrex)
                    if (t.MarketName == Market.MarketName)
                    {
                        return t.MinTradeSize;
                    }
                throw new Exception("Не удалось получить MinTradeSize для " + Market.MarketName);
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Ошибка GetMinTradeSize: " + ex.Message);
                return 0;
            }
        }

        private async Task<string> ExecuteOrder(BaseTypes.Market Market, decimal Amount, decimal Rate, bool DirectionBuy, InvokePrint Print)
        {
            try
            {
                if (DirectionBuy)
                {
                    var accepted = await bittrex.BuyLimit(Market.MarketName, Amount, Rate);
                    return accepted.Uuid;
                }
                else
                {
                    var accepted = await bittrex.SellLimit(Market.MarketName, Amount, Rate);
                    return accepted.Uuid;
                }
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Ошибка BittrexApi ExecuteOrder: " + ex.Message);
                return "";
            }
        }

        private Task CancelOrderAsync(string Uuid, InvokePrint Print)
        {
            return null;
        }

        private string ConvertTime(DateTime Time)
        {
            return String.Format("{{ {0} {1} {2} {3} {4} {5}}}",
                             Time.Year,
                             Time.Month,
                             Time.Day,
                             Time.Hour,
                             Time.Minute,
                             Time.Second);
        }

        private string PrimaryValue(DateTime Time, Candle_Interval Interval)
        {
            string result = "";
            switch (Interval)
            {
                case Candle_Interval.oneMin:
                    result = Time.Minute.ToString();
                    break;
                case Candle_Interval.fiveMin:
                    result = Time.Minute.ToString();
                    break;
                case Candle_Interval.thirtyMin:
                    result = Time.Minute.ToString();
                    break;
                case Candle_Interval.hour:
                    result = Time.Hour.ToString();
                    break;
                case Candle_Interval.day:
                    result = Time.Day.ToString();
                    break;
            }
            return result;
        }

        /// <summary>
        /// Формирование последней свечи по MarketHistory
        /// </summary>
        /// <returns></returns>
        private async Task<List<BaseTypes.Candle>> CandleByMarketHistory(BaseTypes.Market Market, Candle_Interval Interval, InvokePrint Print)
        {
            try
            {
                List<Trade> mhist = await GetMarketHistory(Market, Print);
                Candle lastCandle = await GetLastCandle(Market, Interval, Print);

                List<BaseTypes.Candle> resCandles = new List<BaseTypes.Candle>();
                DateTime NTime = StrategyTool.AddPeriod(lastCandle.Time, Interval);
                if (mhist[0].Timestamp > NTime && ConvertTime(mhist[0].Timestamp) != ConvertTime(NTime))
                {
                    resCandles.Add(new BaseTypes.Candle
                    {
                        Open = mhist[0].Price,
                        Low = mhist[0].Price,
                        Hight = mhist[0].Price,
                        Close = mhist[0].Price,
                        Time = NTime
                    });
                }
                else
                {
                    return null;
                }

                List<DateTime> times = new List<DateTime>();
                times.Add(NTime);
                while (times.Last() < mhist[0].Timestamp)
                {
                    times.Add(StrategyTool.AddPeriod(times.Last(), Interval));
                }
                times.RemoveAt(times.Count - 1);
                int Cpos = times.Count - 1;

                decimal Hval = mhist[0].Price;
                decimal Lval = mhist[0].Price;
                for (int i = 1; i < mhist.Count - 1; i++)
                {
                    if (mhist[i].Timestamp < times[Cpos]) //оформление свечи
                    {
                        resCandles.Last().Hight = Hval;
                        resCandles.Last().Low = Lval;
                        resCandles.Last().Open = mhist[i - 1].Price;

                        Hval = mhist[i].Price;
                        Lval = mhist[i].Price;

                        Cpos = Cpos - 1;
                        if (Cpos < 0)
                            break;
                        resCandles.Add(new BaseTypes.Candle
                        {
                            Open = mhist[i].Price,
                            Low = mhist[i].Price,
                            Hight = mhist[i].Price,
                            Close = mhist[i].Price,
                            Time = times[Cpos]
                        });
                    }
                    else //обновление max/min текущей свечи
                    {
                        if (Hval < mhist[i].Price)
                            Hval = mhist[i].Price;
                        if (Lval > mhist[i].Price)
                            Lval = mhist[i].Price;
                    }
                }
                if (Cpos >= 0)
                {
                    resCandles.Last().Hight = Hval;
                    resCandles.Last().Low = Lval;
                    resCandles.Last().Open = mhist[mhist.Count - 1].Price;
                }
                resCandles.Reverse();
                if (resCandles.Count > 0)
                {
                    var tmp = await bittrex.GetTicker(Market.MarketName);
                    decimal lastValue = tmp.Last.Value;

                    if (lastValue > resCandles.Last().Hight)
                        resCandles.Last().Hight = lastValue;
                    else
                    if (lastValue < resCandles.Last().Low)
                        resCandles.Last().Low = lastValue;
                    else
                        resCandles.Last().Close = lastValue;
                }

                return resCandles;
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Ошибка BittrexApi CandleByMarketHistory: " + ex.Message);
                return null;
            }
        }

        private async Task<Candle> GetLastCandle(BaseTypes.Market Market, Candle_Interval Interval, InvokePrint Print)
        {
            try
            {
                var tmp = await bittrex.GetLastCandle(Market.MarketName, Interval.ToString());
                Candle result = new Candle
                {
                    Open = tmp.Open,
                    Hight = tmp.Hight,
                    Low = tmp.Low,
                    Close = tmp.Close,
                    Time = tmp.Time
                };
                return result;
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Ошибка BittrexApi GetLastCandle: " + ex.Message);
                return null;
            }
        }

        private async Task<List<Trade>> GetMarketHistory(BaseTypes.Market Market, InvokePrint Print)
        {
            try
            {
                var tmp = await bittrex.GetMarketHistory(Market.MarketName);
                List<Trade> result = new List<Trade>();
                foreach (var t in tmp)
                    result.Add(new Trade
                    {
                        FillType = t.FillType,
                        Id = t.Id,
                        MarketName = t.MarketName,
                        OrderType = t.OrderType,
                        Price = t.Price,
                        Quantity = t.Quantity,
                        Timestamp = t.Timestamp,
                        Total = t.Total
                    });
                return result;
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Ошибка BittrexApi GetMarketHistory: " + ex.Message);
                return null;
            }
        }

        private async Task<List<HistoricOrder>> GetOrderHistory(BaseTypes.Market Market, InvokePrint Print)
        {
            try
            {
                var tmp = await bittrex.GetOrderHistory(Market.MarketName);
                List<HistoricOrder> result = new List<HistoricOrder>();
                foreach (var t in tmp)
                    result.Add(new HistoricOrder
                    {
                        Commission = t.Commission,
                        Condition = t.Condition,
                        ConditionTarget = t.ConditionTarget,
                        Exchange = t.Exchange,
                        ImmediateOrCancel = t.ImmediateOrCancel,
                        IsConditional = t.IsConditional,
                        Limit = t.Limit,
                        OrderType = t.OrderType,
                        OrderUuid = t.OrderUuid,
                        Price = t.Price,
                        PricePerUnit = t.PricePerUnit,
                        Quantity = t.Quantity,
                        QuantityRemaining = t.QuantityRemaining,
                        Timestamp = t.Timestamp
                    });
                return result;
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Ошибка GetOrderHistory: " + ex.Message);
                return null;
            }
        }

        public void ListenPrice(BaseTypes.Market market, Action<BaseTypes.Ticker> handler, InvokePrint Print)
        {
            try
            {
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Ошибка BittrexApi ListenPrice: " + ex.Message);
            }
        }

        public void CloseListenPrice(BaseTypes.Market market, Action<BaseTypes.Ticker> priceHandler, InvokePrint Print)
        {
        }
    }
}
