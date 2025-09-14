namespace CommitteeSyncService.ApiResponses;

// API Response classes for totals endpoint
public class TotalsApiResponse
{
    public int? epoch_no { get; set; }
    public string? circulation { get; set; }
    public string? treasury { get; set; }
    public string? reward { get; set; }
    public string? supply { get; set; }
    public string? reserves { get; set; }
    public string? fees { get; set; }
    public string? deposits_stake { get; set; }
    public string? deposits_drep { get; set; }
    public string? deposits_proposal { get; set; }
}