namespace ProposalSyncService.ApiResponses;

public class ProposalListApiResponse
{
    public int? block_time { get; set; }
    public string? proposal_id { get; set; }
    public string? proposal_tx_hash { get; set; }
    public int? proposal_index { get; set; }
    public string? proposal_type { get; set; }
    public string? proposal_description { get; set; }
    public string? deposit { get; set; }
    public string? return_address { get; set; }
    public int? proposed_epoch { get; set; }
    public object? ratified_epoch { get; set; }
    public object? enacted_epoch { get; set; }
    public object? dropped_epoch { get; set; }
    public object? expired_epoch { get; set; }
    public object? expiration { get; set; }
    public string? meta_url { get; set; }
    public string? meta_hash { get; set; }
    public string? meta_json { get; set; }
    public string? meta_comment { get; set; }
    public string? meta_language { get; set; }
    public string? meta_is_valid { get; set; }
    public string? withdrawal { get; set; }
    public string? param_proposal { get; set; }
}