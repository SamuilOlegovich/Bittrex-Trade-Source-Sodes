using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Binance.API.Csharp.Client.Models.General
{
    public class LotSize
    {
        public string Symbol { get; set; }

        [JsonProperty("minQty")]
        public decimal MinQty { get; set; }
        [JsonProperty("maxQty")]
        public decimal MaxQty { get; set; }
        [JsonProperty("stepSize")]
        public decimal StepSize { get; set; }
    }
}
