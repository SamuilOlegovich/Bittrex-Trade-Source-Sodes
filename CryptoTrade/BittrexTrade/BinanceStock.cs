using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.WebSocket;

namespace CryptoTrade
{
    public class BinanceStock : IStock
    {
        private static ApiClient apiClient;
        private static BinanceClient binanceClient;
        private static Dictionary<string, int> LotSizes = null;

        private static List<WebSocketInstance> AllWebSockets = new List<WebSocketInstance>();
        private static readonly object LockWebSocket = new object();
        private static Timer timerWebSocketsUpdate;

        public BinanceStock()
        {
            if (apiClient == null)
            {
                apiClient = new ApiClient("", "");
                binanceClient = new BinanceClient(apiClient);
            }
            if (LotSizes == null)
            {
                GetLotSizes();
            }

            if (timerWebSocketsUpdate == null)
            {
                timerWebSocketsUpdate = new Timer(TWebSocketsUpdate);
                timerWebSocketsUpdate.Change(TimeSpan.FromSeconds(30), TimeSpan.FromHours(23));
            }
        }

        public static void SetApiKeys(string apiKey, string apiSecret)
        {
            apiClient = new ApiClient(apiKey, apiSecret);
            binanceClient = new BinanceClient(apiClient);
        }

        private void TWebSocketsUpdate(object state)
        {
            RestartWebSocket();
        }

        public void RestartWebSocket()
        {
            try
            {
                lock (LockWebSocket)
                {
                    var ToRemove = new List<WebSocketInstance>();
                    foreach (var winst in AllWebSockets)
                    {
                        if (!winst.Restart())
                        {
                            ToRemove.Add(winst);
                            Form1.Print($"Error in restart WebSocket for {winst.CurMarket.MarketName}");
                        }
                    }

                    foreach (var tmp in ToRemove)
                    {
                        AllWebSockets.Remove(tmp);

                        if (!AllWebSockets.Exists((x) => x.CurMarket.MarketName == tmp.CurMarket.MarketName))
                        {
                            AllWebSockets.Add(new WebSocketInstance(tmp.CurMarket));
                        }

                        foreach (var thandler in tmp.CurPriceActions)
                        {
                            AllWebSockets.First((x) => x.CurMarket.MarketName == tmp.CurMarket.MarketName).ListenPrice(thandler, Form1.Print);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Form1.Print($"Error in RestartWebSocket: {ex.Message}");
            }
        }

        public string GetStockName()
        {
            return "Binance";
        }

        public async Task ShowAccountRest(InvokePrint Print)
        {
            var sinfo = await binanceClient.GetServerTime();
            Print(String.Format("ServerTime: {0} - {1}", sinfo.ServerTime, TimeFromUTC(sinfo.ServerTime)), false);
            var ainfo = await binanceClient.GetAccountInfo();
            Print(String.Format("CanTrade: {0}\r\nCanWithdraw: {1}\r\nCanDeposit: {2}",
                ainfo.CanTrade, ainfo.CanWithdraw, ainfo.CanDeposit), false);
        }

        private async void GetLotSizes()
        {
            try
            {
                var ListLSizes = await binanceClient.GetLotSizes();
                LotSizes = new Dictionary<string, int>(ListLSizes.Count);
                foreach (var tmp in ListLSizes)
                {
                    int PrecSize = (int)Math.Log10(1d / (double)tmp.StepSize);
                    if (PrecSize < 0)
                    {
                        Form1.Print(String.Format("Error in binance api GetLotSizes. StepSize: {0}, Symbol: {1}, MinQty: {2}, PrecSize: {3}",
                            tmp.StepSize, tmp.Symbol, tmp.MinQty, PrecSize));
                        LotSizes.Add(tmp.Symbol, 3);
                    }
                    else
                    {
                        LotSizes.Add(tmp.Symbol, PrecSize);
                    }
                }
            }
            catch (Exception ex)
            {
                Thread.Sleep(500);
                System.Media.SystemSounds.Beep.Play();
                ThreadPool.QueueUserWorkItem((x) => Form1.Print("Error in binance api GetLotSizes: " + ex.Message));
            }
        }

        public async Task<List<BaseTypes.Market>> GetMarkets(InvokePrint Print)
        {
            try
            {
                var wer = await binanceClient.GetAllPairs();

                var resMarkets = new List<BaseTypes.Market>();
                foreach (string ms in wer)
                {
                    resMarkets.Add(new BaseTypes.Market(ms));
                }
                return resMarkets;
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Ошибка BinanceApi GetMarkets: " + ex.Message);
                return null;
            }
        }

        public async Task<List<BaseTypes.CurrencyBalance>> GetAllBalances(InvokePrint Print)
        {
            try
            {
                var ainfo = await binanceClient.GetAccountInfo();

                var balances = new List<BaseTypes.CurrencyBalance>();
                foreach (var t in ainfo.Balances)
                {
                    balances.Add(new BaseTypes.CurrencyBalance
                    {
                        Currency = t.Asset,
                        Balance = t.Free + t.Locked,
                        Available = t.Free
                    });
                }

                return balances;
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Ошибка BinanceApi GetAllBalances: " + ex.Message);
                return null;
            }
        }

        public async Task<BaseTypes.CurrencyBalance> GetBalance(string Currency, InvokePrint Print)
        {
            try
            {
                var ainfo = await binanceClient.GetAccountInfo();
                var Bbalance = ainfo.Balances.First(x => x.Asset == Currency);

                var balance = new BaseTypes.CurrencyBalance
                {
                    Currency = Bbalance.Asset,
                    Balance = Bbalance.Free + Bbalance.Locked,
                    Available = Bbalance.Free
                };
                return balance;
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print(String.Format("Ошибка BinanceApi GetBalance: {0}\r\nCurrency: {1}.", ex.Message, Currency));
                return null;
            }
        }

        public async Task<BaseTypes.TradeResult> ExecuteMarket(BaseTypes.Market Market, double Amount, bool DirectionBuy, InvokePrint Print)
        {
            decimal amount = 0;
            try
            {
                decimal tamount = (decimal)Amount;
                int lsize = LotSizes[Market.MarketName.Replace("-", "")];
                amount = Math.Round(tamount, lsize);

                var oside = DirectionBuy ? OrderSide.BUY : OrderSide.SELL;
                var MarketOrder = await binanceClient.PostNewOrder(Market.MarketName.Replace("-", ""),
                    amount, 0m, oside, OrderType.MARKET);

                var tresult = new BaseTypes.TradeResult()
                {
                    Symbol = MarketOrder.Symbol,
                    OrderId = MarketOrder.OrderId.ToString(),

                    IsFilled = MarketOrder.Status == "FILLED",
                    BaseQty = MarketOrder.ExecutedQty,
                    MarketQty = MarketOrder.СummulativeQuoteQty
                };

                decimal sum1 = 0;
                decimal sum2 = 0;
                foreach (var fill in MarketOrder.Fills)
                {
                    sum1 += fill.Qty * fill.Price;
                    sum2 += fill.Qty;
                }
                tresult.AveragePrice = Math.Round(sum1 / sum2, 8);

                return tresult;
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print(String.Format("Ошибка BinanceApi ExecuteMarket: {0}.\r\nMarket: {1}, Amount: {2}, DirectionBuy: {3}.\r\namount: {4}.",
                   ex.Message, Market.MarketName, Amount, DirectionBuy, amount));
                return null;
            }
        }

        public async Task<BaseTypes.Ticker> GetMarketPrice(BaseTypes.Market Market, InvokePrint Print)
        {
            try
            {
                var asdf = await binanceClient.GetPriceChange24H(Market.MarketName.Replace("-", ""));

                BaseTypes.Ticker ticker = null;
                foreach (var tmp in asdf)
                {
                    if (tmp.Symbol == Market.MarketName.Replace("-", ""))
                    {
                        ticker = new BaseTypes.Ticker
                        {
                            Ask = tmp.AskPrice,
                            Bid = tmp.BidPrice,
                        };
                        break;
                    }
                }
                return ticker;
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print(String.Format("Ошибка BinanceApi GetMarketPrice: {0}.\r\nMarket: {1}.",
                    ex.Message, Market.ToString()));
                return null;
            }
        }

        //max Limit=1000
        public async Task<List<BaseTypes.Candle>> GetCandles(BaseTypes.Market Market, Candle_Interval Interval, int Limit, InvokePrint Print)
        {
            try
            {
                var tinterval = GetTimeInterval(Interval);
                var BCandles = await binanceClient.GetCandleSticks(Market.MarketName.Replace("-", ""), tinterval, null, null, Limit);

                var Candles = new List<BaseTypes.Candle>();
                foreach (var t in BCandles)
                {
                    Candles.Add(new BaseTypes.Candle
                    {
                        Open = t.Open,
                        Hight = t.High,
                        Low = t.Low,
                        Close = t.Close,
                        Time = TimeFromUTC(t.OpenTime)
                    });
                }

                return Candles;
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print(String.Format("Ошибка BinanceApi GetCandles: {0}.\r\nMarket: {1}, Interval: {2}",
                    ex.Message, Market.ToString(), Interval.ToString()));
                return null;
            }
        }

        public async Task<BaseTypes.TradeResult> GetOrderAmount(string OrderID, InvokePrint Print)
        {
            try
            {
                string[] arrtmp = OrderID.Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
                long orderID = Convert.ToInt64(arrtmp[1]);

                var ResOrder = await binanceClient.GetOrder(arrtmp[0], orderID);
                var tresult = new BaseTypes.TradeResult
                {
                    IsFilled = ResOrder.Status == "FILLED"
                };
                tresult.BaseQty = ResOrder.ExecutedQty;
                tresult.MarketQty = ResOrder.СummulativeQuoteQty;

                tresult.AveragePrice = ResOrder.Price;
                return tresult;
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Ошибка BinanceApi OrderAmount: " + ex.Message);
                return null;
            }
        }

        private TimeInterval GetTimeInterval(Candle_Interval cinterval)
        {
            var tinterv = TimeInterval.Hours_1;
            switch (cinterval)
            {
                case Candle_Interval.oneMin:
                    tinterv = TimeInterval.Minutes_1;
                    break;
                case Candle_Interval.fiveMin:
                    tinterv = TimeInterval.Minutes_5;
                    break;
                case Candle_Interval.thirtyMin:
                    tinterv = TimeInterval.Minutes_30;
                    break;
                case Candle_Interval.hour:
                    tinterv = TimeInterval.Hours_1;
                    break;
                case Candle_Interval.day:
                    tinterv = TimeInterval.Days_1;
                    break;
            }
            return tinterv;
        }

        private DateTime TimeFromUTC(long UTCtime)
        {
            //TimeSpan timeSpan = TimeSpan.FromMilliseconds(UTCtime);
            //DateTime resDateTime = new DateTime(timeSpan.Ticks);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddMilliseconds(UTCtime);
        }

        public void ListenPrice(BaseTypes.Market market, Action<BaseTypes.Ticker> handler, InvokePrint Print)
        {
            try
            {
                lock (LockWebSocket)
                {
                    if (!AllWebSockets.Exists((x) => x.CurMarket.MarketName == market.MarketName))
                    {
                        AllWebSockets.Add(new WebSocketInstance(market));
                    }
                    AllWebSockets.First((x) => x.CurMarket.MarketName == market.MarketName).ListenPrice(handler, Print);
                }
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Ошибка BinanceApi ListenPrice: " + ex.Message);
            }
        }

        public void CloseListenPrice(BaseTypes.Market market, Action<BaseTypes.Ticker> priceHandler, InvokePrint Print)
        {
            try
            {
                lock (LockWebSocket)
                {
                    var winstance = AllWebSockets.First(x => x.CurMarket.MarketName == market.MarketName);
                    winstance.CloseListenPrice(priceHandler, Print);
                    if (winstance.CurPriceActions.Count == 0)
                    {
                        AllWebSockets.Remove(winstance);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Ошибка BinanceApi CloseListenPrice: " + ex.Message);
            }
        }

        private class WebSocketInstance
        {
            public readonly BaseTypes.Market CurMarket;
            private readonly string StrMarket = "";
            public string WebSocketID = "";
            private readonly object LocalLockWebSocket;
            public List<Action<BaseTypes.Ticker>> CurPriceActions = new List<Action<BaseTypes.Ticker>>();
            private bool InHandler = false;

            public WebSocketInstance(BaseTypes.Market market)
            {
                LocalLockWebSocket = new object();
                CurMarket = market;
                StrMarket = market.MarketName.Replace("-", "").ToLower();
            }

            public void ListenPrice(Action<BaseTypes.Ticker> handler, InvokePrint Print)
            {
                try
                {
                    lock (LocalLockWebSocket)
                    {
                        CurPriceActions.Add(handler);
                        if (WebSocketID == "")
                        {
                            WebSocketID = binanceClient.ListenDepthEndpoint(StrMarket, (x) => { }, depthHandler);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Media.SystemSounds.Beep.Play();
                    Print("Ошибка LBinanceApi ListenPrice: " + ex.Message);
                }
            }

            private void depthHandler(DepthMessage dmessage)
            {
                if (InHandler)
                {
                    return;
                }
                InHandler = true;

                var resTicker = new BaseTypes.Ticker()
                {
                    Ask = dmessage.Asks.ElementAt(0).Price,
                    Bid = dmessage.Bids.ElementAt(0).Price
                };
                lock (LocalLockWebSocket)
                {
                    foreach (var thand in CurPriceActions)
                    {
                        thand(resTicker);
                    }
                }
                InHandler = false;
            }

            public void CloseListenPrice(Action<BaseTypes.Ticker> priceHandler, InvokePrint Print)
            {
                try
                {
                    lock (LocalLockWebSocket)
                    {
                        CurPriceActions.Remove(priceHandler);
                        if (CurPriceActions.Count == 0)
                        {
                            binanceClient._apiClient.CloseWebSocket(WebSocketID);
                            WebSocketID = "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Media.SystemSounds.Beep.Play();
                    Print("Ошибка LBinanceApi CloseListenPrice: " + ex.Message);
                }
            }

            public bool Restart()
            {
                lock (LocalLockWebSocket)
                {
                    if (String.IsNullOrEmpty(WebSocketID))
                    {
                        return false;
                    }
                    return binanceClient._apiClient.RestartWebSocket(WebSocketID);
                }
            }
        }
    }
}
