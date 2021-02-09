using Newtonsoft.Json;

namespace Binance.API.Csharp.Client.Models.Account
{
    public class OneFill
    {
        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("qty")]
        public decimal Qty { get; set; }

        [JsonProperty("commission")]
        public decimal Commission { get; set; }

        [JsonProperty("commissionAsset")]
        public string commissionAsset { get; set; }

        [JsonProperty("tradeId")]
        public string TradeId { get; set; }
    }
}
