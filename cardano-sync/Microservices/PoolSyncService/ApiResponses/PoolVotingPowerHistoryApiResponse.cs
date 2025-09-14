using System.Text.Json.Serialization;

namespace PoolSyncService.ApiResponses;

public class PoolVotingPowerHistoryApiResponse
{
    [JsonPropertyName("pool_id_bech32")]
    public string? pool_id_bech32 { get; set; }

    [JsonPropertyName("epoch_no")]
    public int? epoch_no { get; set; }

    [JsonPropertyName("amount")]
    public string? amount { get; set; }
}