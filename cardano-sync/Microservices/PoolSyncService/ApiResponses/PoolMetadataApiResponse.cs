using System.Text.Json.Serialization;

namespace PoolSyncService.ApiResponses
{
    public class PoolMetadataApiResponse
    {
        [JsonPropertyName("pool_id_bech32")]
        public string? pool_id_bech32 { get; set; }

        [JsonPropertyName("meta_url")]
        public string? meta_url { get; set; }

        [JsonPropertyName("meta_hash")]
        public string? meta_hash { get; set; }

        [JsonPropertyName("meta_json")]
        public string? meta_json { get; set; }
    }
}
