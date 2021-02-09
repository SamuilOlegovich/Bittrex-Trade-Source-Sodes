using Newtonsoft.Json;
using System.Collections.Generic;

namespace Binance.API.Csharp.Client.Models.Account
{
    public class NewOrder
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }
        [JsonProperty("orderId")]
        public long OrderId { get; set; }
        [JsonProperty("clientOrderId")]
        public string ClientOrderId { get; set; }
        [JsonProperty("transactTime")]
        public long TransactTime { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("executedQty")]
        public decimal ExecutedQty { get; set; }
        [JsonProperty("cummulativeQuoteQty")]
        public decimal СummulativeQuoteQty { get; set; }

        public List<OneFill> Fills { get; set; }
    }
}
