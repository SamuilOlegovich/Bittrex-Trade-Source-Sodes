using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoTrade.BaseTypes
{
    public class Candle
    {
        public decimal Open { get; set; }
        public decimal Hight { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public DateTime Time { get; set; }
    }
}
