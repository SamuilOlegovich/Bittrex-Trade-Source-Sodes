using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;

namespace CryptoTrade
{
    public partial class Settings : Form
    {
        private FormSettings FSettings;
        private InvokePrint Print;
        private Action<FormSettings> OnChangeSettings;

        public Settings(FormSettings settings, Action<FormSettings> onChangeSettings)
        {
            InitializeComponent();

            this.Print = Form1.Print;
            this.OnChangeSettings = onChangeSettings;
            this.FSettings = settings;
            foreach (var sname in FSettings.AllStocks)
            {
                comboBox1.Items.Add(sname);
            }
            if (comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;
            textBox1.Text = settings.FormName;
        }

        /// <summary>
        /// Сохранить
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            string sname = comboBox1.SelectedItem.ToString(); //внести изменения в выбранную биржу
            bool NeedUpdate = false;

            FormSettings.ApiKeys CurApiKeys = FSettings.AllApiKeys.First(t => t.StockName == sname);
            if (CurApiKeys == null)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Ошибка: ключи апи для " + sname + " не найдены!");
                return;
            }
            string NewPublic = textBox6.Text.Trim();
            string NewSecret = textBox5.Text.Trim();
            if (CurApiKeys.Public != NewPublic || CurApiKeys.Secret != NewSecret) //применить измененния
            {
                CurApiKeys.Public = NewPublic;
                CurApiKeys.Secret = NewSecret;
                SetApiKeys(CurApiKeys);
                NeedUpdate = true;
            }
            if(FSettings.FormName != textBox1.Text)
            {
                FSettings.FormName = textBox1.Text;
                NeedUpdate = true;
            }

            if (NeedUpdate)
            {
                OnChangeSettings(FSettings);
                IniTool.ChangeValueParam("FormSettings", FSettings.DataAsDictionary());
            }
        }

        private void SetApiKeys(FormSettings.ApiKeys ApiKeys)
        {
            IStock stock = (this.Owner as Form1).AllStocks.First(x => x.GetStockName() == ApiKeys.StockName);
            Type stocktype = stock.GetType();
            MethodInfo MInfo = stocktype.GetMethod("SetApiKeys", BindingFlags.Public | BindingFlags.Static);
            MInfo.Invoke(null, new object[] { ApiKeys.Public, ApiKeys.Secret });
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox6.Text = "";
            textBox5.Text = "";
            string sname = comboBox1.SelectedItem.ToString();
            FormSettings.ApiKeys CurApiKeys = FSettings.AllApiKeys.First(t => t.StockName == sname);
            textBox6.Text = CurApiKeys.Public;
            textBox5.Text = CurApiKeys.Secret;
        }
    }
}
