using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoTrade.BaseTypes;

namespace CryptoTrade
{
    public interface IStock
    {
        string GetStockName();

        Task<List<Market>> GetMarkets(InvokePrint Print);

        Task<List<CurrencyBalance>> GetAllBalances(InvokePrint Print);

        Task<CurrencyBalance> GetBalance(string Currency, InvokePrint Print);

        Task<Ticker> GetMarketPrice(Market Market, InvokePrint Print);

        //Amount передается в BaseCurrency для sell и buy
        Task<TradeResult> ExecuteMarket(Market Market, double Amount, bool DirectionBuy, InvokePrint Print);

        Task<TradeResult> GetOrderAmount(string OrderID, InvokePrint Print);

        Task<List<Candle>> GetCandles(Market Market, Candle_Interval Interval, int Limit, InvokePrint Print);

        void ListenPrice(Market market, Action<Ticker> handler, InvokePrint Print);

        void CloseListenPrice(Market market, Action<Ticker> priceHandler, InvokePrint Print);

        void RestartWebSocket();
    }
}
