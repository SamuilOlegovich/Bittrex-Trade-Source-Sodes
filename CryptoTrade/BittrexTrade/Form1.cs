using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using CryptoTrade.BaseTypes;
using IniParser;
using IniParser.Model;

namespace CryptoTrade
{
    public delegate void InvokePrint(string Text, bool PrintTime = true, bool TelegramSend = true);

    public partial class Form1 : Form, ILanguageChangable
    {
        public static InvokePrint Print;
        public static string ProgName = "";
        public List<IStock> AllStocks = new List<IStock>();

        private static FormSettings FSetting = new FormSettings();
        private StrategyManager SManager;

        private Dictionary<string, string> AllStrategies = new Dictionary<string, string>() //StrategyName, ShowName
        { { "Сетка", "Grid"},
          { "Пересечeние МА", "CrossMA" },
          { "Limit-Limit", "Limit-Limit"} };

        public Form1()
        {
            InitializeComponent();

            Print = new InvokePrint(MPrint);
            SManager = new StrategyManager(Print, OnActiveStartegyChangeState);
            foreach (var strat in AllStrategies)
            {
                strategyType.Items.Add(strat.Key);
            }
            strategyType.SelectedIndex = 0;
            strategyInterval.SelectedIndex = 0;
            Languages.SelectedIndex = 0;

            ReadFormSettings();
            ChangeSetting(FSetting);

            foreach (var StockKeys in FSetting.AllApiKeys)
            {
                var StockObject = ExTool.StockByName(StockKeys.StockName);
                if (StockObject != null)
                {
                    var stocktype = StockObject.GetType();
                    var MInfo = stocktype.GetMethod("SetApiKeys", BindingFlags.Public | BindingFlags.Static);
                    MInfo.Invoke(null, new object[] { StockKeys.Public, StockKeys.Secret });

                    AllStocks.Add(StockObject);
                }
                else
                {
                    System.Media.SystemSounds.Beep.Play();
                    Print("Ошибка: тип биржи " + StockKeys.StockName + " не найдено!");
                }
            }
            foreach (var stock in AllStocks)
            {
                comboBox1.Items.Add(stock.GetStockName());
            }
            comboBox1.SelectedIndex = 0;

            SManager.ReadStrategies();
            foreach (string item in SManager.StrategiesList.Keys)
            {
                listBox2.Items.Add(item);
            }
            if (listBox2.Items.Count > 0)
            {
                listBox2.SelectedIndex = listBox2.Items.Count - 1;
            }
        }

        private void ApplyResource(Control control, ComponentResourceManager resources, CultureInfo CInfo)
        {
            foreach (Control c in control.Controls)
            {
                if (c.HasChildren)
                {
                    ApplyResource(c, resources, CInfo);
                }
                resources.ApplyResources(c, c.Name, CInfo);
            }
        }

        public void ChangeFormLanguage(AvaliableLocalizations newLocalization)
        {
            LanguageSettings.getInstance().SetCulture(newLocalization);

            var resources = new ComponentResourceManager(typeof(Form1));
            var newCultureInfo = new CultureInfo(EnumDescriptionHelper.GetEnumDescription(newLocalization));
            resources.ApplyResources(this, "$this", newCultureInfo);
            ApplyResource(this, resources, newCultureInfo);

            //Print("Set to: " + newCultureInfo.NativeName);
            SetCurrenLanguageButtonChecked(newCultureInfo.NativeName);
        }

        private void SetCurrenLanguageButtonChecked(string cname)
        {
            foreach (object item in Languages.Items)
            {
                if (item.ToString() == cname)
                {
                    Languages.SelectedItem = item;
                    break;
                }
            }
        }

        /// <summary>
        /// Прочитать глобальные настройки из ini файла
        /// </summary>
        private void ReadFormSettings()
        {
            if (File.Exists(IniTool.IniFnameParams))
            {
                try
                {
                    var data = IniTool.ReadSectionParams("FormSettings");
                    FSetting.LoadFromDictionary(data);
                }
                catch (Exception ex)
                {
                    System.Media.SystemSounds.Beep.Play();
                    Print("Ошибка чтения FormSettings: " + ex.Message);
                    FSetting = new FormSettings();
                    IniTool.ChangeValueParam("FormSettings", FSetting.DataAsDictionary());
                    return;
                }
            }
            else
            {
                var fileIniData = new FileIniDataParser();
                var newParsedData = new IniData();
                fileIniData.WriteFile(IniTool.IniFnameParams, newParsedData);
                FSetting = new FormSettings();
                IniTool.ChangeValueParam("FormSettings", FSetting.DataAsDictionary());
            }
        }

        /// <summary>
        /// Выаод информации
        /// </summary>
        /// <param name="text"></param>
        private void MPrint(string text, bool ptime, bool TelegramSend = true)
        {
            if (String.IsNullOrEmpty(text))
            {
                return;
            }

            if (TelegramSend)
            {
                TelegramBot.Send($"<{ProgName}> {text}");
            }

            string ptext = String.Format("{0}{1}{2}", ptime == false ? "" : DateTime.Now.ToString() + " - ", text, Environment.NewLine);

            if (textBoxLogsWindow.InvokeRequired)
            {
                textBoxLogsWindow.Invoke(new Action<string>((str) =>
                {
                    textBoxLogsWindow.AppendText(str);
                }), ptext);
            }
            else
            {
                textBoxLogsWindow.AppendText(ptext);
            }
        }

        /// <summary>
        /// Получает результат запуска или остановки стратегии.
        /// </summary>
        /// <param name="state"></param>
        private void OnActiveStartegyChangeState(bool FromRun, bool Result)
        {
            if (FromRun == true)
            {
                if (Result == true)
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            buttonRun.Enabled = false;
                            buttonContinue.Enabled = false;
                            buttonStop.Enabled = true;
                        }));
                    }
                    else
                    {
                        buttonRun.Enabled = false;
                        buttonContinue.Enabled = false;
                        buttonStop.Enabled = true;
                    }
                    Print(String.Format("Стратегия [{0}] запущена успешно.", SManager.ActiveStrategy.Param.StrategyName), true);
                }
                else
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            buttonRun.Enabled = true;
                            buttonContinue.Enabled = true;
                            buttonStop.Enabled = false;
                        }));
                    }
                    else
                    {
                        buttonRun.Enabled = true;
                        buttonContinue.Enabled = true;
                        buttonStop.Enabled = false;
                    }
                    Print(String.Format("Стратегию [{0}] не запущено.", SManager.ActiveStrategy.Param.StrategyName), true);
                }
            }
            else
            {
                if (Result == true)
                {
                    Print(String.Format("Стратегия [{0}] остановлена успешно.", SManager.ActiveStrategy.Param.StrategyName), true);
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            buttonRun.Enabled = true;
                            buttonContinue.Enabled = true;
                            buttonStop.Enabled = false;
                        }));
                    }
                    else
                    {
                        buttonRun.Enabled = true;
                        buttonContinue.Enabled = true;
                        buttonStop.Enabled = false;
                    }
                }
                else
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            buttonRun.Enabled = false;
                            buttonContinue.Enabled = false;
                            buttonStop.Enabled = true;
                        }));
                    }
                    else
                    {
                        buttonRun.Enabled = false;
                        buttonContinue.Enabled = false;
                        buttonStop.Enabled = true;
                    }
                    Print(String.Format("Стратегию [{0}] не остановлено.", SManager.ActiveStrategy.Param.StrategyName), true);
                }
            }
        }

        /// <summary>
        /// Очистить лог
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            textBoxLogsWindow.Clear();
        }

        /// <summary>
        /// Список торговых пар
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button2_Click(object sender, EventArgs e)
        {
            try
            {
                var Stock = AllStocks.First(x => x.GetStockName() == comboBox1.SelectedItem.ToString());
                var markets = await Stock.GetMarkets(Print);
                if (markets == null)
                {
                    return;
                }

                markets = markets.OrderBy(x => x.MarketName).ToList();
                listBox1.Items.Clear();
                foreach (var t in markets)
                {
                    listBox1.Items.Add(t.MarketName);
                }
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print(ex.Message + "\r\n", true);
            }
        }

        /// <summary>
        /// Настройки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button10_Click(object sender, EventArgs e)
        {
            var fset = new Settings(FSetting, ChangeSetting);
            fset.ShowDialog(this);
        }

        private void ChangeSetting(FormSettings fSetting)
        {
            ProgName = fSetting.FormName;
            Text = ProgName;
            TelegramBot.SetParam(FSetting.TelegramToken, FSetting.TelegramChatId);
            TelegramBot.Send($"Program <{ProgName}> is running.");
        }

        /// <summary>
        /// Состояние стратегии на данный момент
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            Print(SManager?.ActiveStrategy.ShowInfo(), true, false);
        }

        /// <summary>
        /// Запустить
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonRun_Click(object sender, EventArgs e)
        {
            var CurStrategy = SManager.ActiveStrategy;
            if (CurStrategy == null)
            {
                return;
            }

            try
            {
                string OldName = CurStrategy.Param.StrategyName;
                var dictParam = IniTool.ReadSectionParams(CurStrategy.UniqueID);
                CurStrategy.Param.LoadData(dictParam);
                CheckStrategyRename(OldName, CurStrategy.Param.StrategyName);
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print(String.Format("Ошибка чтения настроек из ini файла: {0}", ex.Message));
                return;
            }
            ThreadPool.QueueUserWorkItem((x) => CurStrategy.Start(true));
        }

        /// <summary>
        /// Продолжить
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click_1(object sender, EventArgs e)
        {
            var CurStrategy = SManager.ActiveStrategy;
            if (CurStrategy == null)
            {
                return;
            }

            try
            {
                string OldName = CurStrategy.Param.StrategyName;
                var dictParam = IniTool.ReadSectionParams(CurStrategy.UniqueID);
                CurStrategy.Param.LoadData(dictParam);
                CheckStrategyRename(OldName, CurStrategy.Param.StrategyName);

                var dictState = IniTool.ReadSectionStates(CurStrategy.UniqueID);
                CurStrategy.State.LoadData(dictState);
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print(String.Format("Ошибка чтения настроек из ini файла: {0}", ex.Message));
                return;
            }
            ThreadPool.QueueUserWorkItem((x) => CurStrategy.Start(false));
        }

        /// <summary>
        /// Остановить
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonStop_Click(object sender, EventArgs e)
        {
            var CurStrategy = SManager.ActiveStrategy;
            if (CurStrategy == null)
            {
                return;
            }
            ThreadPool.QueueUserWorkItem((x) => CurStrategy.Stop());
        }

        /// <summary>
        /// Удалить
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonDelete_Click(object sender, EventArgs e)
        {
            var CurStrategy = SManager.ActiveStrategy;
            if (CurStrategy == null)
            {
                return;
            }
            if (CurStrategy.State.IsStartegyRun)
            {
                buttonStop_Click(null, null);
                var tbegin = DateTime.UtcNow;
                while (DateTime.UtcNow.Subtract(tbegin) < TimeSpan.FromSeconds(5) && CurStrategy.State.IsStartegyRun)
                {
                    Thread.Sleep(200);
                }

                if (CurStrategy.State.IsStartegyRun)
                {
                    var result = MessageBox.Show("Не удалось остановить стратегию. Все равно удалить?", "Удаление стратегии",
                        MessageBoxButtons.YesNo);
                    if (result == DialogResult.No)
                    {
                        return;
                    }
                }
            }

            string sectname = CurStrategy.UniqueID;
            IniTool.RemoveFromParams(sectname);
            IniTool.RemoveFromStates(sectname);
            SManager.RemoveStrategy(CurStrategy.Param.StrategyName);

            int index = listBox2.SelectedIndex;
            listBox2.Items.RemoveAt(index);
            if (listBox2.Items.Count > 0)
            {
                listBox2.SelectedIndex = (index - 1) >= 0 ? index - 1 : 0;
            }
        }

        /// <summary>
        /// Добавить стратегию
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonAddStrategy_Click(object sender, EventArgs e)
        {
            var ReadParam = ReadStrategyParam(false);
            SManager.AddNewStrategy(ReadParam);

            listBox2.Items.Add(ReadParam.StrategyName);
            listBox2.SelectedIndex = listBox2.Items.Count - 1;
        }

        /// <summary>
        /// Считать параметры стратегии с формы
        /// </summary>
        /// <returns></returns>
        private StrategyParam ReadStrategyParam(bool ForSave)
        {
            try
            {
                if (strategyName.Text == "" || strategyMarket.Text == "" || strategyBuyValue.Text == ""
                    || strategySellValue.Text == "")
                {
                    System.Media.SystemSounds.Beep.Play();
                    Print("Заполнены не все поля!", false);
                    return null;
                }

                if (!SManager.CheckNameValid(strategyName.Text, ForSave))
                {
                    System.Media.SystemSounds.Beep.Play();
                    Print("Такое название уже существует!", false);
                    return null;
                }

                Market sMarket = null;
                try
                {
                    sMarket = Market.LoadFromString(strategyMarket.Text);
                }
                catch (Exception ex)
                {
                    System.Media.SystemSounds.Beep.Play();
                    Print("Не удалось считать торговую пару! " + ex.Message);
                    return null;
                }

                //ограничения покупки
                double ValueRestBuy = 0;
                if (!String.IsNullOrEmpty(strategyBuyValue.Text))
                {
                    ValueRestBuy = Convert.ToDouble(strategyBuyValue.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
                }
                var balRestBuy = new ExTool.StepRepresent(1)
                {
                    Values = new List<double>() { ValueRestBuy },
                    IsPercentSize = strategyBuyIsPerc.Checked ? true : false
                };

                //ограничения продажи
                double ValueRestSell = 0;
                if (!String.IsNullOrEmpty(strategySellValue.Text))
                {
                    ValueRestSell = Convert.ToDouble(strategySellValue.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
                }

                var balRestSell = new ExTool.StepRepresent(1)
                {
                    Values = new List<double>() { ValueRestSell },
                    IsPercentSize = strategySellIsPerc.Checked ? true : false,
                };

                var Param = new StrategyParam
                {
                    StrategyName = strategyName.Text,
                    StrategyType = AllStrategies[strategyType.SelectedItem.ToString()],
                    Market = sMarket,
                    BalanceRestBuy = balRestBuy,
                    BalanceRestSell = balRestSell,
                    Interval = (Candle_Interval)strategyInterval.SelectedIndex,
                    SellOnlyBought = strategySellBought.Checked,
                    WriteToFile = checkBox2.Checked,
                    Stock = ExTool.StockByName(comboBox1.SelectedItem.ToString())
                };
                return Param;
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Ошибка считывания параметров: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Cохранить
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button12_Click(object sender, EventArgs e)
        {
            var CurStrategy = SManager.ActiveStrategy;
            if (CurStrategy == null)
            {
                return;
            }

            var ReadParam = ReadStrategyParam(true);
            if (ReadParam == null)
            {
                return;
            }

            string OldName = CurStrategy.Param.StrategyName;
            StrategyTool.ChangeBaseParam(ref CurStrategy, ReadParam);
            CheckStrategyRename(OldName, CurStrategy.Param.StrategyName);
            CurStrategy.SaveData();
        }

        private void CheckStrategyRename(string OldName, string NewName)
        {
            if (OldName != NewName)
            {
                SManager.RenameStrategy(OldName, NewName);
                for (int i = 0; i < listBox2.Items.Count; i++)
                {
                    if (listBox2.Items[i].ToString() == OldName)
                    {
                        listBox2.Items[i] = NewName;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Описание параметров стратегии
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            var CurStrategy = SManager.ActiveStrategy;
            if (CurStrategy == null)
            {
                return;
            }

            var sb = new StringBuilder();
            sb.AppendFormat("Параметры:\r\n");
            foreach (var t in CurStrategy.Param.Description)
            {
                sb.AppendFormat("{0}: {1}\r\n", t.Key, t.Value);
            }

            sb.AppendFormat("\r\nСостояние:\r\n");
            foreach (var t in CurStrategy.State.Description)
            {
                sb.AppendFormat("{0}: {1}\r\n", t.Key, t.Value);
            }
            MPrint(sb.ToString(), true);
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex >= 0)
            {
                SManager.SetStrategyActive(listBox2.SelectedItem.ToString());
                SetActiveStrategy(SManager.ActiveStrategy);
            }
        }

        /// <summary>
        /// Смена текущей выбранной стратегии для редактирования
        /// </summary>
        /// <param name="strategy"></param>
        private void SetActiveStrategy(Strategy strategy)
        {
            if (strategy == null)
            {
                buttonRun.Enabled = false;
                buttonContinue.Enabled = false;
                buttonStop.Enabled = false;
                buttonDelete.Enabled = false;

                strategyName.Text = "";
                strategyMarket.Text = "";

                strategyBuyIsPerc.Checked = true;
                strategyBuyValue.Text = "";

                strategySellIsPerc.Checked = true;
                strategySellValue.Text = "";

                strategyInterval.SelectedIndex = 0;
                strategyType.SelectedIndex = 0;
                comboBox1.SelectedIndex = 0;

                strategySellBought.Checked = false;
                checkBox2.Checked = false;
                return;
            }
            //управление
            buttonRun.Enabled = !strategy.State.IsStartegyRun;
            buttonContinue.Enabled = !strategy.State.IsStartegyRun;
            buttonStop.Enabled = strategy.State.IsStartegyRun;
            buttonDelete.Enabled = true;
            //параметры
            strategyName.Text = strategy.Param.StrategyName;
            strategyMarket.Text = strategy.Param.Market.MarketName;

            if (strategy.Param.BalanceRestBuy.IsPercentSize)
            {
                strategyBuyIsPerc.Checked = true;
            }
            else
            {
                strategyBuyIsValue.Checked = true;
            }
            strategyBuyValue.Text = strategy.Param.BalanceRestBuy.Values[0].ToString();

            if (strategy.Param.BalanceRestSell.IsPercentSize)
            {
                strategySellIsPerc.Checked = true;
            }
            else
            {
                strategySellIsValue.Checked = true;
            }
            strategySellValue.Text = strategy.Param.BalanceRestSell.Values[0].ToString();

            strategyInterval.SelectedIndex = (int)strategy.Param.Interval;

            try
            {
                strategyType.SelectedItem = AllStrategies.First(x => x.Value == strategy.Param.StrategyType).Key;
            }
            catch
            {
                strategyType.SelectedIndex = -1;
                System.Media.SystemSounds.Beep.Play();
                Print(String.Format("Тип стратегии {0} не обнаружено!", strategy.Param.StrategyType));
            }

            strategySellBought.Checked = strategy.Param.SellOnlyBought;
            checkBox2.Checked = strategy.Param.WriteToFile;

            try
            {
                comboBox1.SelectedIndex = comboBox1.Items.IndexOf(strategy.Param.Stock.GetStockName());
            }
            catch
            {
                comboBox1.SelectedIndex = -1;
                System.Media.SystemSounds.Beep.Play();
                Print(String.Format("Тип биржи {0} не обнаружено!", strategy.Param.Stock.GetStockName()));
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            strategyMarket.Text = (string)listBox1.SelectedItem;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            var CurStrategy = SManager.ActiveStrategy;
            if (CurStrategy == null)
            {
                return;
            }
            CurStrategy.Param.WriteToFile = checkBox2.Checked;
        }

        private void Languages_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Languages.SelectedItem.ToString() == "English")
            {
                ChangeFormLanguage(AvaliableLocalizations.English);
            }

            if (Languages.SelectedItem.ToString() == "Русский")
            {
                ChangeFormLanguage(AvaliableLocalizations.Russian);
            }
        }

        private BinanceStock bstock;
        private Dictionary<Market, Action<Ticker>> adict = new Dictionary<Market, Action<Ticker>>();

        //Test1
        private void button3_Click(object sender, EventArgs e)
        {
            bstock = new BinanceStock();
            var markets = new List<string>()
            {
                "ADX-ETH",
                "AGI-BNB",
                "ADX-ETH",
                "BTC-USDT"
            };

            foreach (string item in markets)
            {
                for (int i = 0; i < 10; i++)
                {
                    var market = new Market(item);
                    string smname = market.MarketName + (i + 1).ToString();
                    var unicAction = new Action<Ticker>((x) =>
                    {
                        ThreadPool.QueueUserWorkItem((xv) => Print(String.Format("{0} - {1} - {2}",
                            smname, x.Ask, x.Bid)));
                        //double tmp = (double)(x.Ask + x.Bid) / 2;
                    });
                    adict.Add(market, unicAction);
                    bstock.ListenPrice(market, unicAction, Print);
                }
            }
        }

        //Test2
        private void button7_Click(object sender, EventArgs e)
        {
            foreach (var tmpact in adict)
            {
                bstock.CloseListenPrice(tmpact.Key, tmpact.Value, Print);
            }
        }

        //Баланс
        private async void button5_Click(object sender, EventArgs e)
        {
            var Stock = AllStocks.First(x => x.GetStockName() == comboBox1.SelectedItem.ToString());
            var allBalance = await Stock.GetAllBalances(Print);
            if (allBalance == null)
            {
                return;
            }

            var sb = new StringBuilder();
            foreach (var bal in allBalance)
            {
                if (bal.Available == 0)
                {
                    continue;
                }

                sb.AppendFormat("{0} - {1}\r\n", bal.Currency, bal.Available);
            }
            Print(String.Format("Баланс на {0}:\r\n{1}", Stock.GetStockName(), sb.ToString()));
        }

        //Активные ордера
        private void button8_Click(object sender, EventArgs e)
        {
            SManager.ShowOrdersTable(this);
        }

        private void strategyType_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            TelegramBot.DisposeResources();
        }
    }
}
