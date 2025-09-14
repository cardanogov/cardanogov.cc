namespace SharedLibrary.DTOs
{
    public class ProposalDto
    {
        public string? id { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
        public string? status { get; set; }
        public string? start_date { get; set; }
        public string? end_date { get; set; }
        public double? voting_power { get; set; }
        public int? total_votes { get; set; }
        public int? yes_votes { get; set; }
        public int? no_votes { get; set; }
        public int? abstain_votes { get; set; }
        public string? created_by { get; set; }
        public string? created_at { get; set; }
        public string? updated_at { get; set; }
    }

    public class ProposalResponseDto
    {
        public List<ProposalDto>? data { get; set; }
        public int? total { get; set; }
        public int? page { get; set; }
        public int? page_size { get; set; }
    }

    public class ProposalStatsResponseDto
    {
        public int? totalProposals { get; set; }
        public int? approvedProposals { get; set; }
        public double? approvalRate { get; set; }
        public double? percentage_change { get; set; }
        public double? difference { get; set; }
        public int? live_proposals { get; set; }
        public int? expired_proposals { get; set; }
        public int? enacted_proposals { get; set; }
    }

    public class ProposalInfoResponseDto
    {
        public string? proposalId { get; set; }
        public int? proposedEpoch { get; set; }
        public int? expiration { get; set; }
        public string? imageUrl { get; set; }
        public string? title { get; set; }
        public string? proposalType { get; set; }
        public string? abstract_ { get; set; }
        public string? hash { get; set; }
        public string? status { get; set; }
        public string? motivation { get; set; }
        public string? rationale { get; set; }
        public string? anchorLink { get; set; }
        public string? supportLink { get; set; }
        public string? timeLine { get; set; }
        public string? time { get; set; }
        public double? deposit { get; set; }
        public string? startTime { get; set; }
        public string? endTime { get; set; }
        public long? block_time { get; set; }


        // Additional properties for detailed voting information
        public TotalVoterDto? totalVoter { get; set; }

        public double? drepYesVotes { get; set; }
        public double? drepYesPct { get; set; }
        public double? drepNoVotes { get; set; }
        public double? drepNoPct { get; set; }
        public double? drepActiveNoVotePower { get; set; }
        public double? drepNoConfidence { get; set; }
        public double? drepAbstainAlways { get; set; }
        public double? drepAbstainActive { get; set; }

        public double? poolYesVotes { get; set; }
        public double? poolYesPct { get; set; }
        public double? poolNoVotes { get; set; }
        public double? poolNoPct { get; set; }
        public double? poolActiveNoVotePower { get; set; }
        public double? poolNoConfidence { get; set; }
        public double? poolAbstainAlways { get; set; }
        public double? poolAbstainActive { get; set; }

        public double? committeeYesPct { get; set; }
        public string? param_proposal { get; set; }
    }

    // New DTO classes for voting details
    public class TotalVoterDto
    {
        public VoterCountsDto? drep { get; set; }
        public VoterCountsDto? spo { get; set; }
        public VoterCountsDto? cc { get; set; }
    }

    public class VoterCountsDto
    {
        public int? yes { get; set; }
        public int? no { get; set; }
        public int? abstain { get; set; }
        public int? abstainAlways { get; set; }

    }


    public class GovernanceActionResponseDto
    {
        public int? total_proposals { get; set; }
        public int? approved_proposals { get; set; }
        public double? percentage_change { get; set; }
        public List<ProposalInfoResponseDto>? proposal_info { get; set; }
    }

    public class ProposalVotingSummaryResponseDto
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

    public class GovernanceActionsStatisticsResponseDto
    {
        public Dictionary<string, int>? statistics { get; set; }
    }

    public class GovernanceActionsStatisticsByEpochResponseDto
    {
        public Dictionary<string, Dictionary<string, int>>? statistics_by_epoch { get; set; }
    }

    public class ProposalVotingResponseDto
    {
        public string? block_time { get; set; }
        public string? voter_role { get; set; }
        public string? voter_id { get; set; }
        public string? voter_hex { get; set; }
        public bool? voter_has_script { get; set; }
        public string? vote { get; set; }
        public string? meta_url { get; set; }
        public string? meta_hash { get; set; }
        public string? proposal_type { get; set; }
        public string? proposal_id { get; set; }
    }

    public class ProposalVotesResponseDto
    {
        public int? totalVotesResult { get; set; }
        public List<ProposalVotesDto>? voteInfo { get; set; }
    }

    public class ProposalVotesDto
    {
        public string? block_time { get; set; }
        public string? name { get; set; }
        public string? voter_role { get; set; }
        public double? voting_power { get; set; }
        public string? vote { get; set; }
        public string? voter_id { get; set; }
    }

    public class ProposalActionTypeResponseDto
    {
        public string? proposal_id { get; set; }
        public string? proposal_type { get; set; }
        public object? meta_json { get; set; }
    }
}