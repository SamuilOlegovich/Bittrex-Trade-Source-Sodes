using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoTrade
{
    public class FormSettings
    {
        private Object LockObj = new Object();

        public class ApiKeys
        {
            public string StockName { get; set; }
            public string Public { get; set; }
            public string Secret { get; set; }
        }
        public readonly List<string> AllStocks = new List<string> { "Binance", "Bittrex" };
        public List<ApiKeys> AllApiKeys = new List<ApiKeys>();
        public string FormName = "CryptoTrade v0.022";
        public string TelegramToken = "";
        public string TelegramChatId = "";

        public FormSettings()
        {
            foreach (var item in AllStocks)
            {
                AllApiKeys.Add(new ApiKeys
                {
                    StockName = item,
                    Public = "",
                    Secret = ""
                });
            }
        }

        public Dictionary<string, string> DataAsDictionary()
        {
            Dictionary<string, string> KeysValues = new Dictionary<string, string>();
            foreach (var stockName in AllStocks)
            {
                ApiKeys CurApiKey = AllApiKeys.First(x => x.StockName == stockName);
                KeysValues.Add(stockName + "ApiKey", CurApiKey.Public);
                KeysValues.Add(stockName + "ApiSecret", CurApiKey.Secret);
            }

            KeysValues.Add("FormName", FormName);
            KeysValues.Add("TelegramToken", TelegramToken);
            KeysValues.Add("TelegramChatId", TelegramChatId);
            return KeysValues;
        }

        public void LoadFromDictionary(Dictionary<string, string> data)
        {
            foreach (var stockName in AllStocks)
            {
                ApiKeys CurApiKey = AllApiKeys.First(x => x.StockName == stockName);
                CurApiKey.Public = data[CurApiKey.StockName + "ApiKey"];
                CurApiKey.Secret = data[CurApiKey.StockName + "ApiSecret"];
            }

            FormName = data["FormName"];
            TelegramToken = data["TelegramToken"];
            TelegramChatId = data["TelegramChatId"];
        }
    }
}
