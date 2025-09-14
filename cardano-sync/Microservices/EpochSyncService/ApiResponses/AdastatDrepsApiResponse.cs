using System.Text.Json.Serialization;

namespace EpochSyncService.ApiResponses;

public class AdastatDrepsApiResponse
{
    [JsonPropertyName("data")]
    public AdastatDrepsData? Data { get; set; }

    [JsonPropertyName("rows")]
    public AdastatDrepRow[]? Rows { get; set; }

    [JsonPropertyName("cursor")]
    public AdastatDrepsCursor? Cursor { get; set; }

    [JsonPropertyName("code")]
    public int Code { get; set; }
}

public class AdastatDrepsData
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("live_stake")]
    public string? LiveStake { get; set; }

    [JsonPropertyName("delegator")]
    public int Delegator { get; set; }
}

public class AdastatDrepRow
{
    [JsonPropertyName("hash")]
    public string? Hash { get; set; }

    [JsonPropertyName("bech32_legacy")]
    public string? Bech32Legacy { get; set; }

    [JsonPropertyName("has_script")]
    public bool HasScript { get; set; }

    [JsonPropertyName("tx_hash")]
    public string? TxHash { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("payment_address")]
    public string? PaymentAddress { get; set; }

    [JsonPropertyName("given_name")]
    public string? GivenName { get; set; }

    [JsonPropertyName("objectives")]
    public string? Objectives { get; set; }

    [JsonPropertyName("motivations")]
    public string? Motivations { get; set; }

    [JsonPropertyName("qualifications")]
    public string? Qualifications { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("live_stake")]
    public string? LiveStake { get; set; }

    [JsonPropertyName("delegator")]
    public int Delegator { get; set; }

    [JsonPropertyName("tx_time")]
    public long? TxTime { get; set; }

    [JsonPropertyName("last_active_epoch")]
    public int? LastActiveEpoch { get; set; }

    [JsonPropertyName("bech32")]
    public string? Bech32 { get; set; }
}

public class AdastatDrepsCursor
{
    [JsonPropertyName("after")]
    public string? After { get; set; }

    [JsonPropertyName("next")]
    public bool Next { get; set; }
}