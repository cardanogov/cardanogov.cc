namespace SharedLibrary.DTOs
{
    public class TotalInfoResponseDto
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

    public class SpoVotingPowerHistoryResponseDto
    {
        public string? ticker { get; set; }
        public double? active_stake { get; set; }
        public string? pool_status { get; set; }
        public double? percentage { get; set; }
        public string? group { get; set; }
    }

    public class AdaStatisticsResponseDto
    {
        public List<PoolResultDto>? pool_result { get; set; }
        public List<DrepResultDto>? drep_result { get; set; }
        public List<SupplyResultDto>? supply_result { get; set; }
    }

    public class PoolResultDto
    {
        public int? epoch_no { get; set; }
        public string? total_active_stake { get; set; }
    }

    public class DrepResultDto
    {
        public int? epoch_no { get; set; }
        public string? amount { get; set; }
    }

    public class SupplyResultDto
    {
        public int? epoch_no { get; set; }
        public string? supply { get; set; }
    }

    public class AdaStatisticsPercentageResponseDto
    {
        public string? ada_staking { get; set; }
        public double? ada_staking_percentage { get; set; }
        public string? ada_register_to_vote { get; set; }
        public double? ada_register_to_vote_percentage { get; set; }
        public string? circulating_supply { get; set; }
        public double? circulating_supply_percentage { get; set; }
        public string? ada_abstain { get; set; }
        public double? ada_abstain_percentage { get; set; }
    }

    public class PoolDto
    {
        public string? ticker { get; set; }
        public string? pool_status { get; set; }
        public double? active_stake { get; set; }
        public string? pool_id_bech32 { get; set; }
        public int? active_epoch_no { get; set; }
        public string? meta_url { get; set; }
        public int? delegator { get; set; }
        public double? voting_amount { get; set; }
        public long? block_time { get; set; }
        public int? vote { get; set; }
        public string? homepage { get; set; }
        public string? status { get; set; }
        public string? references { get; set; }
    }

    public class PoolInfoDto
    {
        public string? ticker { get; set; }
        public string? pool_id_bech32 { get; set; }
        public List<VotingPowerDto>? voting_power { get; set; }
        public PoolStatusDto? status { get; set; }
        public PoolInformationDto? information { get; set; }
        public List<VoteInfoDto>? vote_info { get; set; }
        public List<DelegationDto>? delegation { get; set; }
        public List<RegistrationDto>? registration { get; set; }
    }

    public class VotingPowerDto
    {
        public int? epoch_no { get; set; }
        public double? amount { get; set; }
    }

    public class PoolStatusDto
    {
        public int? registration { get; set; }
        public int? last_activity { get; set; }
        public string? status { get; set; }
    }

    public class PoolInformationDto
    {
        public string? description { get; set; }
        public string? name { get; set; }
        public string? ticker { get; set; }
        public double? live_stake { get; set; }
        public double? deposit { get; set; }
        public double? margin { get; set; }
        public double? fixed_cost { get; set; }
        public int? active_epoch_no { get; set; }
        public int? block_count { get; set; }
        public int? created { get; set; }
        public int? delegators { get; set; }
    }

    public class VoteInfoDto
    {
        public string? proposal_id { get; set; }
        public string? title { get; set; }
        public string? proposal_type { get; set; }
        public int? block_time { get; set; }
        public string? vote { get; set; }
        public string? meta_url { get; set; }
    }

    public class DelegationDto
    {
        public string? stake_address { get; set; }
        public double? amount { get; set; }
        public string? latest_delegation_tx_hash { get; set; }
        public int? block_time { get; set; }
    }

    public class RegistrationDto
    {
        public int? block_time { get; set; }
        public string? ticker { get; set; }
        public string? meta_url { get; set; }
    }

    public class PoolResponseDto
    {
        public List<PoolDto>? items { get; set; }
        public int? total { get; set; }
        public int? pageNumber { get; set; }
        public int? pageSize { get; set; }
    }

    public class DelegationResponseDto
    {
        public List<DelegationDto>? items { get; set; }
        public int? total { get; set; }
        public int? pageNumber { get; set; }
        public int? pageSize { get; set; }
    }
}