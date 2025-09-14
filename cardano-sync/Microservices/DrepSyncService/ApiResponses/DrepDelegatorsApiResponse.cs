using System.Text.Json.Serialization;

namespace DrepSyncService.ApiResponses
{
    public class DrepDelegatorsApiResponse
    {
        [JsonPropertyName("stake_address")]
        public string? stake_address { get; set; }

        [JsonPropertyName("stake_address_hex")]
        public string? stake_address_hex { get; set; }

        [JsonPropertyName("script_hash")]
        public string? script_hash { get; set; }

        [JsonPropertyName("epoch_no")]
        public int? epoch_no { get; set; }

        [JsonPropertyName("amount")]
        public string? amount { get; set; }
    }
}