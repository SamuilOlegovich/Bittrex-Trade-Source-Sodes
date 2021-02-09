using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoTrade.BaseTypes
{
    public class Market
    {
        public string MarketName { get; set; }
        public string MarketCurrency { get; set; }
        public string BaseCurrency { get; set; }

        public Market()
        {

        }

        public Market(string MarketName)
        {
            string[] sarr = MarketName.ToUpper().Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            this.MarketName = MarketName;
            this.BaseCurrency = sarr[0];
            this.MarketCurrency = sarr[1];
        }

        public static Market LoadFromString(string SMarket)
        {
            string[] sarr = SMarket.ToUpper().Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (sarr.Length != 2)
                throw new Exception("Не удалось получить объект Market из строки" + SMarket);

            return new Market
            {
                MarketName = SMarket,
                BaseCurrency = sarr[0],
                MarketCurrency = sarr[1]
            };
        }

        public override string ToString()
        {
            return String.Format("MarketName: {0}, MarketCurrency: {1}, BaseCurrency: {2}.",
                MarketName, MarketCurrency, BaseCurrency);
        }
    }
}
