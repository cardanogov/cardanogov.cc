namespace SharedLibrary.DTOs
{
    public class VotingCardInfoDto
    {
        public double? currentRegister { get; set; }
        public string? registerChange { get; set; }
        public string? registerRate { get; set; }
        public double? abstainAmount { get; set; }
        public string? abstainChange { get; set; }
        public double? currentStake { get; set; }
        public string? stakeChange { get; set; }
        public double? currentSuplly { get; set; }
        public string? supplyChange { get; set; }
        public string? supplyRate { get; set; }
    }

    public class VotingHistoryResponseDto
    {
        public int? totalVote { get; set; }
        public List<VotingHistoryDto>? filteredVoteInfo { get; set; }
    }

    public class VotingHistoryDto
    {
        public double? amount { get; set; }
        public string? block_time { get; set; }
        public int? epoch_no { get; set; }
        public string? name { get; set; }
        public string? proposal_type { get; set; }
        public string? vote { get; set; }
        public string? voter_id { get; set; }
        public string? voter_role { get; set; }
    }

    public class VoteListResponseDto
    {
        public string? vote_tx_hash { get; set; }
        public string? voter_role { get; set; }
        public string? voter_id { get; set; }
        public string? proposal_id { get; set; }
        public string? proposal_tx_hash { get; set; }
        public int? proposal_index { get; set; }
        public string? proposal_type { get; set; }
        public int? epoch_no { get; set; }
        public int? block_height { get; set; }
        public long? block_time { get; set; }
        public string? vote { get; set; }
        public string? meta_url { get; set; }
        public string? meta_hash { get; set; }
        public string? meta_json { get; set; }
    }

    public class VoteStatisticResponseDto
    {
        public int? epoch_no { get; set; }
        public List<VoteStatisticDto>? sum_yes_voting_power { get; set; }
        public List<VoteStatisticDto>? sum_no_voting_power { get; set; }
    }

    public class VoteStatisticDto
    {
        public string? id { get; set; }
        public double? power { get; set; }
        public long? block_time { get; set; }
        public string? name { get; set; }
        public int? epoch_no { get; set; }
    }

    public class VotingResultResponseDto
    {
        public List<VotingResultDto>? voting_result { get; set; }
    }

    public class VotingResultDto
    {
        public int? epoch_no { get; set; }
        public double? yes_votes { get; set; }
        public double? no_votes { get; set; }
        public double? abstain_votes { get; set; }
    }

    public class VotingPowerResponseDto
    {
        public List<DrepVotingPowerDataDto>? drep_result { get; set; }
        public List<PoolVotingPowerDataDto>? pool_result { get; set; }
    }

    public class DrepVotingPowerDataDto
    {
        public int? epoch_no { get; set; }
        public double? total_power { get; set; }
    }

    public class PoolVotingPowerDataDto
    {
        public int? epoch_no { get; set; }
        public double? total_power { get; set; }
    }
}