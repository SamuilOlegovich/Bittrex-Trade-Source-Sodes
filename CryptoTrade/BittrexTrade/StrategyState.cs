using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace CryptoTrade
{
    public class StrategyState : IStrategyState
    {
        public bool IsStartegyRun { get; set; }
        public decimal PurchasedAmount { get; set; }

        public Dictionary<string, string> Description { get; }

        public StrategyState()
        {
            Description = new Dictionary<string, string>
            {
                { "IsStartegyRun", "Стратегия зупущена или нет (true/false)."},
                { "PurchasedAmount", "Количество купленных монет."}
            };
        }

        public virtual void GetData(ref Dictionary<string, string> DataParams)
        {
            DataParams.Add("IsStartegyRun", IsStartegyRun.ToString());
            DataParams.Add("PurchasedAmount", PurchasedAmount.ToString());
        }

        public virtual void LoadData(Dictionary<string, string> dict)
        {
            IsStartegyRun = Convert.ToBoolean(dict["IsStartegyRun"]);
            PurchasedAmount = Convert.ToDecimal(dict["PurchasedAmount"].Replace(',', '.'), CultureInfo.InvariantCulture);
        }

        public virtual void Reset()
        {
            //IsStartegyRun = false;
            PurchasedAmount = 0;
        }
    }
}
