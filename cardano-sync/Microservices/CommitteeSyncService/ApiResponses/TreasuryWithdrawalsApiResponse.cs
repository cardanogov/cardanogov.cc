namespace CommitteeSyncService.ApiResponses;

// API Response classes for treasury_withdrawals endpoint
public class TreasuryWithdrawalsApiResponse
{
    public int? epoch_no { get; set; }
    public long? sum { get; set; }
}