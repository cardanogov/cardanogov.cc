using System.Text.Json.Serialization;

namespace DrepSyncService.ApiResponses
{
    public class DrepMetadataApiResponse
    {
        [JsonPropertyName("drep_id")]
        public string? drep_id { get; set; }

        [JsonPropertyName("hex")]
        public string? hex { get; set; }

        [JsonPropertyName("has_script")]
        public bool? has_script { get; set; }

        [JsonPropertyName("meta_url")]
        public string? meta_url { get; set; }

        [JsonPropertyName("meta_hash")]
        public string? meta_hash { get; set; }

        [JsonPropertyName("meta_json")]
        public string? meta_json { get; set; }

        [JsonPropertyName("bytes")]
        public string? bytes { get; set; }

        [JsonPropertyName("warning")]
        public string? warning { get; set; }

        [JsonPropertyName("language")]
        public string? language { get; set; }

        [JsonPropertyName("comment")]
        public string? comment { get; set; }

        [JsonPropertyName("is_valid")]
        public bool? is_valid { get; set; }
    }
}