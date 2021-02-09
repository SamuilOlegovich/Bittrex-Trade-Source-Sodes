using System;
using System.Collections.Generic;
using System.Text;
using CryptoTrade.BaseTypes;

namespace CryptoTrade
{
    public abstract class Strategy//<TP, TS> //where TP : IStrategyParam where TS : IStrategyState
    {
        protected object LockFile = new object();
        public InvokePrint Print;

        public string UniqueID { get; }
        public virtual IStrategyParam Param { get; } //параметры
        public virtual IStrategyState State { get; }

        public virtual event Action<bool, bool, string> ChangeState; //параметры: bool FromRun(or Stop), bool Result (good or bad), StrategyName
        public abstract void Start(bool AtFirst);
        public abstract void Stop();
        public StrategyTrade CStrategyTrade;
        public StrategyPrices CStrategyPrices;
        public virtual event EventHandler<ActiveOrdersGridEventArgs> ChangeActiveOrders; //изменение ордеров в позиции стратегии

        public Strategy(string uniqueID)
        {
            if (uniqueID == "")
            {
                var rand = new Random();
                string stmp = "";
                for (int i = 0; i < 10; i++)
                {
                    stmp += rand.Next(1, Int32.MaxValue);
                }

                uniqueID = IniTool.ConvertName(stmp);
            }
            UniqueID = uniqueID;
            Print = new InvokePrint(PrintMethod);
        }

        private void PrintMethod(string Text, bool PrintTime = true, bool TelegramSend = true)
        {
            //await Task.Factory.StartNew(() => GPrint(String.Format("[{0}] - ", Param.Market) + text, PrintTime),
            //    TaskCreationOptions.PreferFairness);
            string RText = String.Format("[{0}] - {1}", Param.StrategyName, Text);
            if (TelegramSend)
            {
                TelegramBot.Send($"<{Form1.ProgName}> {RText}");
            }

            if (Param.WriteToFile)
            {
                string ptext = String.Format("{0}{1}", PrintTime == false ? "" : DateTime.Now.ToString() + " - ", RText);
                string fname = String.Format("{0}/Logs/{1}.txt", AppDomain.CurrentDomain.BaseDirectory, Param.StrategyName);
                lock (LockFile)
                {
                    using (var file = new System.IO.StreamWriter(fname, true))
                    {
                        file.WriteLine(ptext);
                    }
                }
                Form1.Print(RText, PrintTime, false);
            }
            else
            {
                Form1.Print(RText, PrintTime, false);
            }
        }

        public void LoadData(Dictionary<string, string> dict) //загрузить параметры и состояние
        {
            try
            {
                Param.LoadData(dict);
                State.LoadData(dict);
            }
            catch (Exception ex)
            {
                Print("Ошибка считывания параметров стратегии: " + ex.Message, true);
            }
        }

        public void SaveData(bool NotOnlyState = true) //сохранить параметры и состояние в файл
        {
            var StrategyData = new Dictionary<string, string>();
            if (NotOnlyState)
            {
                Param.GetData(ref StrategyData);
                IniTool.ChangeValueParam(UniqueID, StrategyData);
                StrategyData.Clear();
            }

            State.GetData(ref StrategyData);
            IniTool.ChangeValueState(UniqueID, StrategyData);
        }

        public virtual string ShowInfo(bool NotOnlyParams = true) //отображение информации
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Состояние для [{0}]:\r\n", Param.StrategyName);

            var RecData = new Dictionary<string, string>();
            sb.Append("Параметры:\r\n");
            Param.GetData(ref RecData);
            foreach (var t in RecData)
            {
                sb.AppendFormat("{0}: {1}\r\n", t.Key, t.Value);
            }

            if (NotOnlyParams)
            {
                RecData.Clear();
                sb.Append("\r\nСостояние:\r\n");
                State.GetData(ref RecData);
                foreach (var t in RecData)
                {
                    sb.AppendFormat("{0}: {1}\r\n", t.Key, t.Value);
                }
            }
            return sb.ToString();
        }

        public virtual void ActiveOrdersInfo()
        {
            ChangeActiveOrders?.Invoke(this, new ActiveOrdersGridEventArgs());
        }

        public virtual void ForceStop()
        {

        }

        //return value in %
        public virtual double GetPositionState()
        {
            return 0;
        }
    }

    public interface IStrategyParam
    {
        string StrategyName { get; set; } //название стратегии
        string StrategyType { get; set; } //тип стратегии
        IStock Stock { get; set; } //биржа
        Market Market { get; set; } //код инструмента
        ExTool.StepRepresent BalanceRestBuy { get; set; } //% или сумма от баланса при покупке
        ExTool.StepRepresent BalanceRestSell { get; set; } //% или сумма от баланса при продаже
        Candle_Interval Interval { get; set; } //интервал свечей
        int FreqUpdate { get; set; } //частота обновлений в мс индикатора
        bool SellOnlyBought { get; set; } //Продавать только купленное
        bool WriteToFile { get; set; } //вывод у файл
        Dictionary<string, string> Description { get; }

        void GetData(ref Dictionary<string, string> DataParams);
        void LoadData(Dictionary<string, string> dict);
    }

    public interface IStrategyState
    {
        bool IsStartegyRun { get; set; }
        decimal PurchasedAmount { get; set; }
        Dictionary<string, string> Description { get; }

        void GetData(ref Dictionary<string, string> DataParams);
        void LoadData(Dictionary<string, string> dict);
        void Reset(); //сброс настроек в 0
    }
}
