using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Reflection;

namespace CryptoTrade
{
    public enum AvaliableLocalizations
    {
        [Description("en-US")]
        English,
        [Description("ru-RU")]
        Russian
    }

    public static class EnumDescriptionHelper
    {
        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes =
              (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }
    }

    interface ILanguageChangable
    {
        void ChangeFormLanguage(AvaliableLocalizations newLocalization);
    }

}
