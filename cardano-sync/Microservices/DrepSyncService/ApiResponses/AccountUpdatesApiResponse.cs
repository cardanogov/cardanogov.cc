namespace DrepSyncService.ApiResponses;

public class AccountUpdatesApiResponse
{
    public string? stake_address { get; set; }
    public List<AccountUpdateRecord>? updates { get; set; }
}

public class AccountUpdateRecord
{
    public string? tx_hash { get; set; }
    public int? epoch_no { get; set; }
    public long? block_time { get; set; }
    public int? epoch_slot { get; set; }
    public string? action_type { get; set; }
    public long? absolute_slot { get; set; }
}