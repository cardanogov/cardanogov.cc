using System.Text.Json.Serialization;

namespace DrepSyncService.ApiResponses;

public class DrepEpochSummaryApiResponse
{
    [JsonPropertyName("epoch_no")]
    public int? EpochNo { get; set; }

    [JsonPropertyName("amount")]
    public string? Amount { get; set; }

    [JsonPropertyName("dreps")]
    public int? Dreps { get; set; }
}