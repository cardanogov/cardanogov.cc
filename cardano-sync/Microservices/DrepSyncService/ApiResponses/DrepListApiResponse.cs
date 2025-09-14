using System.Text.Json.Serialization;

namespace DrepSyncService.ApiResponses;

public class DrepListApiResponse
{
    [JsonPropertyName("drep_id")]
    public string? drep_id { get; set; }

    [JsonPropertyName("hex")]
    public string? hex { get; set; }

    [JsonPropertyName("has_script")]
    public bool? has_script { get; set; }

    [JsonPropertyName("registered")]
    public bool? registered { get; set; }
}
