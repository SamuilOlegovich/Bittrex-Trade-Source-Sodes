using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.WebSocket;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace Binance.API.Csharp.Client.Test
{
    [TestClass]
    public class BinanceTest
    {
        private static readonly ApiClient apiClient = new ApiClient("@YourApiKey", "@YourApiSecret");
        private static BinanceClient binanceClient = new BinanceClient(apiClient, false);

        #region General
        [TestMethod]
        public void TestConnectivity()
        {
            dynamic test = binanceClient.TestConnectivity().Result;
        }

        [TestMethod]
        public void GetServerTime()
        {
            Models.General.ServerInfo serverTime = binanceClient.GetServerTime().Result;
        }
        #endregion

        #region Market Data
        [TestMethod]
        public void GetOrderBook()
        {
            Models.Market.OrderBook orderBook = binanceClient.GetOrderBook("ethbtc").Result;
        }

        [TestMethod]
        public void GetCandleSticks()
        {
            System.Collections.Generic.IEnumerable<Models.Market.Candlestick> candlestick = binanceClient.GetCandleSticks("ethbtc", TimeInterval.Minutes_15, new System.DateTime(2017, 11, 24), new System.DateTime(2017, 11, 26)).Result;
        }

        [TestMethod]
        public void GetAggregateTrades()
        {
            System.Collections.Generic.IEnumerable<Models.Market.AggregateTrade> aggregateTrades = binanceClient.GetAggregateTrades("ethbtc").Result;
        }

        [TestMethod]
        public void GetPriceChange24H()
        {
            System.Collections.Generic.IEnumerable<Models.Market.PriceChangeInfo> singleTickerInfo = binanceClient.GetPriceChange24H("ETHBTC").Result;

            System.Collections.Generic.IEnumerable<Models.Market.PriceChangeInfo> allTickersInfo = binanceClient.GetPriceChange24H().Result;
        }

        [TestMethod]
        public void GetAllPrices()
        {
            System.Collections.Generic.IEnumerable<Models.Market.SymbolPrice> tickerPrices = binanceClient.GetAllPrices().Result;
        }

        [TestMethod]
        public void GetOrderBookTicker()
        {
            System.Collections.Generic.IEnumerable<Models.Market.OrderBookTicker> orderBookTickers = binanceClient.GetOrderBookTicker().Result;
        }
        #endregion

        #region Account Information
        [TestMethod]
        public void PostLimitOrder()
        {
            Models.Account.NewOrder buyOrder = binanceClient.PostNewOrder("KNCETH", 100m, 0.005m, OrderSide.BUY).Result;
            Models.Account.NewOrder sellOrder = binanceClient.PostNewOrder("KNCETH", 1000m, 1m, OrderSide.SELL).Result;
        }

        [TestMethod]
        public void PostMarketOrder()
        {
            Models.Account.NewOrder buyMarketOrder = binanceClient.PostNewOrder("ethbtc", 0.01m, 0m, OrderSide.BUY, OrderType.MARKET).Result;
            Models.Account.NewOrder sellMarketOrder = binanceClient.PostNewOrder("ethbtc", 0.01m, 0m, OrderSide.SELL, OrderType.MARKET).Result;
        }

        [TestMethod]
        public void PostIcebergOrder()
        {
            Models.Account.NewOrder icebergOrder = binanceClient.PostNewOrder("ethbtc", 0.01m, 0m, OrderSide.BUY, OrderType.MARKET, icebergQty: 2m).Result;
        }

        [TestMethod]
        public void PostNewLimitOrderTest()
        {
            dynamic testOrder = binanceClient.PostNewOrderTest("ethbtc", 1m, 0.1m, OrderSide.BUY).Result;
        }

        [TestMethod]
        public void CancelOrder()
        {
            Models.Account.CanceledOrder canceledOrder = binanceClient.CancelOrder("ethbtc", 9137796).Result;
        }

        [TestMethod]
        public void GetCurrentOpenOrders()
        {
            System.Collections.Generic.IEnumerable<Models.Account.Order> openOrders = binanceClient.GetCurrentOpenOrders("ethbtc").Result;
        }

        [TestMethod]
        public void GetOrder()
        {
            Models.Account.Order order = binanceClient.GetOrder("ethbtc", 8982811).Result;
        }

        [TestMethod]
        public void GetAllOrders()
        {
            System.Collections.Generic.IEnumerable<Models.Account.Order> allOrders = binanceClient.GetAllOrders("ethbtc").Result;
        }

        [TestMethod]
        public void GetAccountInfo()
        {
            Models.Market.AccountInfo accountInfo = binanceClient.GetAccountInfo().Result;
        }

        [TestMethod]
        public void GetTradeList()
        {
            System.Collections.Generic.IEnumerable<Models.Account.Trade> tradeList = binanceClient.GetTradeList("ethbtc").Result;
        }

        [TestMethod]
        public void Withdraw()
        {
            Models.Account.WithdrawResponse withdrawResult = binanceClient.Withdraw("AST", 100m, "@YourDepositAddress").Result;
        }

        [TestMethod]
        public void GetDepositHistory()
        {
            Models.Account.DepositHistory depositHistory = binanceClient.GetDepositHistory("neo", DepositStatus.Success).Result;
        }

        [TestMethod]
        public void GetWithdrawHistory()
        {
            Models.Account.WithdrawHistory withdrawHistory = binanceClient.GetWithdrawHistory("neo").Result;
        }
        #endregion

        #region User stream
        [TestMethod]
        public void StartUserStream()
        {
            string listenKey = binanceClient.StartUserStream().Result.ListenKey;
        }

        [TestMethod]
        public void KeepAliveUserStream()
        {
            dynamic ping = binanceClient.KeepAliveUserStream("@ListenKey").Result;
        }

        [TestMethod]
        public void CloseUserStream()
        {
            dynamic resut = binanceClient.CloseUserStream("@ListenKey").Result;
        }
        #endregion

        #region WebSocket

        #region Depth
        private void DepthHandler(DepthMessage messageData)
        {
            DepthMessage depthData = messageData;
        }

        [TestMethod]
        public void TestDepthEndpoint()
        {
            binanceClient.ListenDepthEndpoint("ethbtc", (x) => { }, DepthHandler);
            Thread.Sleep(50000);
        }

        #endregion

        #region Kline
        private void KlineHandler(KlineMessage messageData)
        {
            KlineMessage klineData = messageData;
        }

        [TestMethod]
        public void TestKlineEndpoint()
        {
            binanceClient.ListenKlineEndpoint("ethbtc", (x) => { }, TimeInterval.Minutes_1, KlineHandler);
            Thread.Sleep(50000);
        }
        #endregion

        #region AggregateTrade
        private void AggregateTradesHandler(AggregateTradeMessage messageData)
        {
            AggregateTradeMessage aggregateTrades = messageData;
        }

        [TestMethod]
        public void AggregateTestTradesEndpoint()
        {
            binanceClient.ListenTradeEndpoint("ethbtc", (x) => { }, AggregateTradesHandler);
            Thread.Sleep(50000);
        }

        #endregion

        #region User Info
        private void AccountHandler(AccountUpdatedMessage messageData)
        {
            AccountUpdatedMessage accountData = messageData;
        }

        private void TradesHandler(OrderOrTradeUpdatedMessage messageData)
        {
            OrderOrTradeUpdatedMessage tradesData = messageData;
        }

        private void OrdersHandler(OrderOrTradeUpdatedMessage messageData)
        {
            OrderOrTradeUpdatedMessage ordersData = messageData;
        }

        [TestMethod]
        public void TestUserDataEndpoint()
        {
            binanceClient.ListenUserDataEndpoint(AccountHandler, TradesHandler, OrdersHandler);
            Thread.Sleep(50000);
        }
        #endregion

        #endregion
    }
}
