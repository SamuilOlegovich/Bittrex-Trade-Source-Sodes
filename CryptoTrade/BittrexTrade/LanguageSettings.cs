using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Threading;

namespace CryptoTrade
{
    public class LanguageSettings
    {
        private static LanguageSettings instance;

        public static LanguageSettings getInstance()
        {
            if (instance == null)
                instance = new LanguageSettings();
            return instance;
        }

        public AvaliableLocalizations CurrentLocalization { get; private set; }

        internal CultureInfo GetCulture()
        {
            return new CultureInfo(EnumDescriptionHelper.GetEnumDescription(CurrentLocalization));
        }

        internal void SetCulture(AvaliableLocalizations newLocalization)
        {
            CurrentLocalization = newLocalization;
            Thread.CurrentThread.CurrentUICulture =
                   new CultureInfo(EnumDescriptionHelper.GetEnumDescription(CurrentLocalization));
        }
    }
}
