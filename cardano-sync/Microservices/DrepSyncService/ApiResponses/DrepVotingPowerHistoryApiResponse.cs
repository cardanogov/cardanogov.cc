using System.Text.Json.Serialization;

namespace DrepSyncService.ApiResponses;

public class DrepVotingPowerHistoryApiResponse
{
    [JsonPropertyName("drep_id")]
    public string? drep_id { get; set; }

    [JsonPropertyName("epoch_no")]
    public int? epoch_no { get; set; }

    [JsonPropertyName("amount")]
    public string? amount { get; set; }
}