using Binance.API.Csharp.Client.Models.Market;
using Binance.API.Csharp.Client.Models.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Binance.API.Csharp.Client.Utils
{
    /// <summary>
    /// Class to parse some specific entities.
    /// </summary>
    public class CustomParser
    {
        /// <summary>
        /// Gets the orderbook data and generates an OrderBook object.
        /// </summary>
        /// <param name="orderBookData">Dynamic containing the orderbook data.</param>
        /// <returns></returns>
        public OrderBook GetParsedOrderBook(dynamic orderBookData)
        {
            OrderBook result = new OrderBook
            {
                LastUpdateId = orderBookData.lastUpdateId.Value
            };

            List<OrderBookOffer> bids = new List<OrderBookOffer>();
            List<OrderBookOffer> asks = new List<OrderBookOffer>();

            foreach (JToken item in ((JArray)orderBookData.bids).ToArray())
            {
                bids.Add(new OrderBookOffer() { Price = decimal.Parse(item[0].ToString()), Quantity = decimal.Parse(item[1].ToString()) });
            }

            foreach (JToken item in ((JArray)orderBookData.asks).ToArray())
            {
                asks.Add(new OrderBookOffer() { Price = decimal.Parse(item[0].ToString()), Quantity = decimal.Parse(item[1].ToString()) });
            }

            result.Bids = bids;
            result.Asks = asks;

            return result;
        }

        /// <summary>
        /// Gets the candlestick data and generates an Candlestick object.
        /// </summary>
        /// <param name="candlestickData">Dynamic containing the candlestick data.</param>
        /// <returns></returns>
        public IEnumerable<Candlestick> GetParsedCandlestick(dynamic candlestickData)
        {
            List<Candlestick> result = new List<Candlestick>();

            foreach (JToken item in ((JArray)candlestickData).ToArray())
            {
                result.Add(new Candlestick()
                {
                    OpenTime = long.Parse(item[0].ToString()),
                    Open = decimal.Parse(item[1].ToString().Replace('.', ',')),
                    High = decimal.Parse(item[2].ToString().Replace('.', ',')),
                    Low = decimal.Parse(item[3].ToString().Replace('.', ',')),
                    Close = decimal.Parse(item[4].ToString().Replace('.', ',')),
                    Volume = decimal.Parse(item[5].ToString().Replace('.', ',')),
                    CloseTime = long.Parse(item[6].ToString()),
                    QuoteAssetVolume = decimal.Parse(item[7].ToString().Replace('.', ',')),
                    NumberOfTrades = int.Parse(item[8].ToString()),
                    TakerBuyBaseAssetVolume = decimal.Parse(item[9].ToString().Replace('.', ',')),
                    TakerBuyQuoteAssetVolume = decimal.Parse(item[10].ToString().Replace('.', ','))
                });
            }

            return result;
        }

        public DepthMessage GetParsedDepthMessage(dynamic messageData)
        {
            DepthMessage result = new DepthMessage();
         
            result.UpdateId = messageData.lastUpdateId;
            /*DepthMessage result = new DepthMessage
            {
                //EventType = messageData.e,
                //EventTime = messageData.E,
                //Symbol = messageData.s,
                UpdateId = messageData.lastUpdateId //messageData.u
            };*/

            List<OrderBookOffer> bids = new List<OrderBookOffer>();
            List<OrderBookOffer> asks = new List<OrderBookOffer>();

            JToken[] barray = ((JArray)messageData.bids).ToArray();
            foreach (JToken item in barray)
            {
                bids.Add(new OrderBookOffer()
                {
                    Price = Convert.ToDecimal(item[0].ToString().Replace(',', '.'), CultureInfo.InvariantCulture),
                    Quantity = Convert.ToDecimal(item[1].ToString().Replace(',', '.'), CultureInfo.InvariantCulture)
                });
            }
            JToken[] aarray = ((JArray)messageData.asks).ToArray();
            foreach (JToken item in aarray)
            {
                asks.Add(new OrderBookOffer()
                {
                    Price = Convert.ToDecimal(item[0].ToString().Replace(',', '.'), CultureInfo.InvariantCulture),
                    Quantity = Convert.ToDecimal(item[1].ToString().Replace(',', '.'), CultureInfo.InvariantCulture)
                });
            }

            result.Bids = bids;
            result.Asks = asks;

            return result;
        }
    }
}
