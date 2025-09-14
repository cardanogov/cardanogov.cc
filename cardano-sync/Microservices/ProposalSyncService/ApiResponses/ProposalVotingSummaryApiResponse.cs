namespace ProposalSyncService.ApiResponses;

public class ProposalVotingSummaryApiResponse
{
    public string? proposal_type { get; set; }
    public int? epoch_no { get; set; }
    public int? drep_yes_votes_cast { get; set; }
    public string? drep_active_yes_vote_power { get; set; }
    public string? drep_yes_vote_power { get; set; }
    public double? drep_yes_pct { get; set; }
    public int? drep_no_votes_cast { get; set; }
    public string? drep_active_no_vote_power { get; set; }
    public string? drep_no_vote_power { get; set; }
    public double? drep_no_pct { get; set; }
    public int? drep_abstain_votes_cast { get; set; }
    public string? drep_active_abstain_vote_power { get; set; }
    public string? drep_always_no_confidence_vote_power { get; set; }
    public string? drep_always_abstain_vote_power { get; set; }
    public int? pool_yes_votes_cast { get; set; }
    public string? pool_active_yes_vote_power { get; set; }
    public string? pool_yes_vote_power { get; set; }
    public double? pool_yes_pct { get; set; }
    public int? pool_no_votes_cast { get; set; }
    public string? pool_active_no_vote_power { get; set; }
    public string? pool_no_vote_power { get; set; }
    public double? pool_no_pct { get; set; }
    public int? pool_abstain_votes_cast { get; set; }
    public string? pool_active_abstain_vote_power { get; set; }
    public int? pool_passive_always_abstain_votes_assigned { get; set; }
    public string? pool_passive_always_abstain_vote_power { get; set; }
    public int? pool_passive_always_no_confidence_votes_assigned { get; set; }
    public string? pool_passive_always_no_confidence_vote_power { get; set; }
    public int? committee_yes_votes_cast { get; set; }
    public double? committee_yes_pct { get; set; }
    public int? committee_no_votes_cast { get; set; }
    public double? committee_no_pct { get; set; }
    public int? committee_abstain_votes_cast { get; set; }
}