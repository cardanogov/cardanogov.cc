using System.Text.Json.Serialization;

namespace EpochSyncService.ApiResponses;

public class AdastatEpochApiResponse
{
    [JsonPropertyName("data")]
    public AdastatEpochData? Data { get; set; }

    [JsonPropertyName("rows")]
    public AdastatEpochRow[]? Rows { get; set; }

    [JsonPropertyName("cursor")]
    public AdastatEpochCursor? Cursor { get; set; }

    [JsonPropertyName("code")]
    public int Code { get; set; }
}

public class AdastatEpochData
{
    [JsonPropertyName("epoch_no")]
    public int EpochNo { get; set; }

    [JsonPropertyName("epoch_slot_no")]
    public int EpochSlotNo { get; set; }

    [JsonPropertyName("slot_no")]
    public long SlotNo { get; set; }
}

public class AdastatEpochRow
{
    [JsonPropertyName("no")]
    public int No { get; set; }

    [JsonPropertyName("tx_amount")]
    public string? TxAmount { get; set; }

    [JsonPropertyName("circulating_supply")]
    public string? CirculatingSupply { get; set; }

    [JsonPropertyName("pool")]
    public int? Pool { get; set; }

    [JsonPropertyName("pool_with_block")]
    public int? PoolWithBlock { get; set; }

    [JsonPropertyName("pool_with_stake")]
    public int? PoolWithStake { get; set; }

    [JsonPropertyName("pool_fee")]
    public string? PoolFee { get; set; }

    [JsonPropertyName("reward_amount")]
    public string? RewardAmount { get; set; }

    [JsonPropertyName("stake")]
    public string? Stake { get; set; }

    [JsonPropertyName("delegator")]
    public int? Delegator { get; set; }

    [JsonPropertyName("account")]
    public int? Account { get; set; }

    [JsonPropertyName("account_with_reward")]
    public int? AccountWithReward { get; set; }

    [JsonPropertyName("pool_register")]
    public int? PoolRegister { get; set; }

    [JsonPropertyName("pool_retire")]
    public int? PoolRetire { get; set; }

    [JsonPropertyName("orphaned_reward_amount")]
    public string? OrphanedRewardAmount { get; set; }

    [JsonPropertyName("block_with_tx")]
    public int? BlockWithTx { get; set; }

    [JsonPropertyName("byron")]
    public int? Byron { get; set; }

    [JsonPropertyName("byron_with_amount")]
    public int? ByronWithAmount { get; set; }

    [JsonPropertyName("byron_amount")]
    public string? ByronAmount { get; set; }

    [JsonPropertyName("account_with_amount")]
    public int? AccountWithAmount { get; set; }

    [JsonPropertyName("delegator_with_stake")]
    public int? DelegatorWithStake { get; set; }

    [JsonPropertyName("token")]
    public int? Token { get; set; }

    [JsonPropertyName("token_policy")]
    public int? TokenPolicy { get; set; }

    [JsonPropertyName("token_holder")]
    public int? TokenHolder { get; set; }

    [JsonPropertyName("token_tx")]
    public int? TokenTx { get; set; }

    [JsonPropertyName("out_sum")]
    public string? OutSum { get; set; }

    [JsonPropertyName("fees")]
    public string? Fees { get; set; }

    [JsonPropertyName("tx")]
    public int? Tx { get; set; }

    [JsonPropertyName("block")]
    public int? Block { get; set; }

    [JsonPropertyName("start_time")]
    public long? StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public long? EndTime { get; set; }

    [JsonPropertyName("optimal_pool_count")]
    public int? OptimalPoolCount { get; set; }

    [JsonPropertyName("decentralisation")]
    public double? Decentralisation { get; set; }

    [JsonPropertyName("nonce")]
    public string? Nonce { get; set; }

    [JsonPropertyName("holder_range")]
    public AdastatHolderRange? HolderRange { get; set; }

    [JsonPropertyName("exchange_rate")]
    public double? ExchangeRate { get; set; }
}

public class AdastatHolderRange
{
    [JsonPropertyName("byron")]
    public Dictionary<string, int>? Byron { get; set; }

    [JsonPropertyName("account")]
    public Dictionary<string, int>? Account { get; set; }

    [JsonPropertyName("address")]
    public Dictionary<string, int>? Address { get; set; }

    [JsonPropertyName("delegator")]
    public Dictionary<string, int>? Delegator { get; set; }
}

public class AdastatEpochCursor
{
    [JsonPropertyName("after")]
    public int After { get; set; }

    [JsonPropertyName("next")]
    public bool Next { get; set; }
}