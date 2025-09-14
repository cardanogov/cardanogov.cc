namespace CommitteeSyncService.ApiResponses;

// API Response classes for committee_votes endpoint
public class CommitteeVotesApiResponse
{
    public string? proposal_id { get; set; }
    public string? proposal_tx_hash { get; set; }
    public int? proposal_index { get; set; }
    public string? vote_tx_hash { get; set; }
    public long? block_time { get; set; }
    public string? vote { get; set; }
    public string? meta_url { get; set; }
    public string? meta_hash { get; set; }
}