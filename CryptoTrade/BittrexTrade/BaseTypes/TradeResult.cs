using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoTrade.BaseTypes
{
    public class TradeResult
    {
        public string Symbol { get; set; }
        public string OrderId { get; set; }

        public bool IsFilled { get; set; } //выполнен полностью или нет
        public decimal BaseQty { get; set; } //сколько передавали на ордер в BaseCurrency
        public decimal MarketQty { get; set; }  //сколько получили при выполнении ордера в MarketCurrency
        public decimal AveragePrice { get; set; }  //cредняя цена сделки
    }
}
