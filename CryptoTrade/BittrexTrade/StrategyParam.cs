using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoTrade.BaseTypes;

namespace CryptoTrade
{
    /// <summary>
    /// Общие параметры для всех стратегий
    /// </summary>
    public class StrategyParam : IStrategyParam
    {
        public string StrategyName { get; set; } //название стратегии
        public string StrategyType { get; set; } //тип стратегии
        public IStock Stock { get; set; } //биржа
        public Market Market { get; set; } //код инструмента
        public ExTool.StepRepresent BalanceRestBuy { get; set; } = new ExTool.StepRepresent(1); //% или сумма от баланса при покупке
        public ExTool.StepRepresent BalanceRestSell { get; set; } = new ExTool.StepRepresent(1); //% или сумма от баланса при продаже
        public Candle_Interval Interval { get; set; } //интервал свечей
        public int FreqUpdate { get; set; }
        public bool SellOnlyBought { get; set; } //Продавать только купленное
        public bool WriteToFile { get; set; } //вывод у файл

        public Dictionary<string, string> Description { get; }

        public StrategyParam()
        {
            FreqUpdate = 2000;
            Description = new Dictionary<string, string>
            {
                { "StrategyName", "Название стратегии"},
                { "StrategyType", "Тип стратегии"},
                { "Stock", "Биржа"},
                { "Market", "Рынок"},
                { "BalanceRestBuy","% или сумма от баланса при покупке: [% от баланса]-[сумма]-[использовать %]"},
                { "BalanceRestSell","% или сумма от баланса при продаже: [% от баланса]-[сумма]-[использовать %]"},
                { "Interval","Интервал свечей"},
                { "FreqUpdate", "Частота обновлений в мс индикатора" },
                { "SellOnlyBought","Продавать только купленное"},
                { "WriteToFile","Вывод у файл вместо формы"}
            };
        }

        public virtual void GetData(ref Dictionary<string, string> DataParams)
        {
            DataParams.Add("StrategyName", StrategyName);
            DataParams.Add("StrategyType", StrategyType);
            DataParams.Add("Stock", Stock.GetStockName());
            DataParams.Add("Market", Market.MarketName);
            DataParams.Add("BalanceRestBuy", BalanceRestBuy.StringRepresent());
            DataParams.Add("BalanceRestSell", BalanceRestSell.StringRepresent());
            DataParams.Add("Interval", Interval.ToString());
            DataParams.Add("FreqUpdate", FreqUpdate.ToString());
            DataParams.Add("SellOnlyBought", SellOnlyBought.ToString());
            DataParams.Add("WriteToFile", WriteToFile.ToString());
        }

        public virtual void LoadData(Dictionary<string, string> dict)
        {
            StrategyName = dict["StrategyName"];
            StrategyType = dict["StrategyType"];
            Stock = ExTool.StockByName(dict["Stock"]);
            Market = Market.LoadFromString(dict["Market"]);
            BalanceRestBuy = ExTool.StepRepresent.LoadFromString(dict["BalanceRestBuy"]);
            BalanceRestSell = ExTool.StepRepresent.LoadFromString(dict["BalanceRestSell"]);
            if (Enum.TryParse(dict["Interval"], out Candle_Interval interval))
            {
                Interval = interval;
            }
            else
            {
                throw new Exception("не удалось считать интервал.");
            }
            FreqUpdate = Convert.ToInt32(dict["FreqUpdate"]);
            SellOnlyBought = Convert.ToBoolean(dict["SellOnlyBought"]);
            WriteToFile = Convert.ToBoolean(dict["WriteToFile"]);
        }
    }
}
