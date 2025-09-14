using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.DTOs
{
    public class TotalDrepResponseDto
    {
        public double? total_active { get; set; }
        public double? total_no_confidence { get; set; }
        public double? total_abstain { get; set; }
        public double? total_register { get; set; }
        public List<int>? chart_stats { get; set; }
    }

    public class DrepInfoResponseDto
    {
        public string? drep_id { get; set; }
        public string? hex { get; set; }
        public bool? has_script { get; set; }
        public bool? registered { get; set; }
        public string? deposit { get; set; }
        public bool? active { get; set; }
        public int? expires_epoch_no { get; set; }
        public string? amount { get; set; }
        public string? meta_url { get; set; }
        public string? meta_hash { get; set; }
    }

    public class DrepVotingPowerHistoryResponseDto
    {
        public string? givenName { get; set; }
        public string? drep_id { get; set; }
        public double? amount { get; set; }
        public double? percentage { get; set; }
    }

    public class DrepHistoryResponseDto
    {
        public string? drep_id { get; set; }
        public int? epoch_no { get; set; }
        public string? amount { get; set; }
    }

    public class DrepDelegatorsResponseDto
    {
        public string? drep_id { get; set; }
        public string? stake_address { get; set; }
        public string? stake_address_hex { get; set; }
        public string? script_hash { get; set; }
        public int? epoch_no { get; set; }
        public string? amount { get; set; }
    }

    public class DrepPoolVotingThresholdResponseDto
    {
        public double? motion_no_confidence { get; set; }
        public double? committee_normal { get; set; }
        public double? committee_no_confidence { get; set; }
        public double? hard_fork_initiation { get; set; }
        public double? update_to_constitution { get; set; }
        public double? network_param_voting { get; set; }
        public double? economic_param_voting { get; set; }
        public double? technical_param_voting { get; set; }
        public double? governance_param_voting { get; set; }
        public double? treasury_withdrawal { get; set; }
        public double? pool_motion_no_confidence { get; set; }
        public double? pool_committee_normal { get; set; }
        public double? pool_committee_no_confidence { get; set; }
        public double? pool_hard_fork_initiation { get; set; }
    }

    public class DrepPoolStakeThresholdResponseDto
    {
        public double drepTotalStake { get; set; }
        public double poolTotalStake { get; set; }
    }

    public class DrepCardDataResponseDto
    {
        public int? dreps { get; set; }
        public string? drepsChange { get; set; }
        public double? totalDelegatedDrep { get; set; }
        public string? totalDelegatedDrepChange { get; set; }
        public double? currentTotalActive { get; set; }
        public string? totalActiveChange { get; set; }
    }

    public class DrepCardDataByIdResponseDto
    {
        public string? givenName { get; set; }
        public double? votingPower { get; set; }
        public double? previousVotingPower { get; set; }
        public double? votingPowerChange { get; set; }
        public string? image { get; set; }
        public string? objectives { get; set; }
        public string? motivations { get; set; }
        public string? qualifications { get; set; }
        public string? references { get; set; }
        public string? registrationDate { get; set; }
    }

    public class ReferencesDto
    {
        public string? name { get; set; }
        public string? url { get; set; }
    }

    public class DrepVoteInfoResponseDto
    {
        public string? proposal_id { get; set; }
        public string? proposal_tx_hash { get; set; }
        public string? vote_tx_hash { get; set; }
        public int? proposal_index { get; set; }
        public string? vote { get; set; }
        public string? meta_url { get; set; }
        public string? meta_hash { get; set; }
        public long? block_time { get; set; }
        public string? drep_id { get; set; }
        public string? proposal_type { get; set; }
        public string? proposal_title { get; set; }
    }

    public class DrepDelegationResponseDto
    {
        public int? total_delegators { get; set; }
        public List<DrepDelegationTableResponseDto>? delegation_data { get; set; }
    }

    public class DrepDelegationTableResponseDto
    {
        public string? stake_address { get; set; }
        public long? block_time { get; set; }
        public double? amount { get; set; }
    }

    public class DrepRegistrationTableResponseDto
    {
        public long? block_time { get; set; }
        public string? given_name { get; set; }
        public string? action { get; set; }
        public string? meta_url { get; set; }
    }

    public class DrepDetailsVotingPowerResponseDto
    {
        public int? epoch_no { get; set; }
        public double? amount { get; set; }
    }

    public class DrepListResponseDto
    {
        public int? total_dreps { get; set; }
        public List<DrepListDto>? drep_info { get; set; }
    }

    public class DrepListDto
    {
        public string? name { get; set; }
        public string? drep_id { get; set; }
        public double? voting_power { get; set; }
        public int? delegators { get; set; }
        public string? active_until { get; set; }
        public string? image { get; set; }
        public int? times_voted { get; set; }
        public string? status { get; set; }
        public string? contact { get; set; }
    }

    public class DrepsVotingPowerResponseDto
    {
        public List<VotingPowerDataDto>? abstain_data { get; set; }
        public List<VotingPowerDataDto>? no_confident_data { get; set; }
        public List<VotingPowerDataDto>? total_drep_data { get; set; }
    }

    public class VotingPowerDataDto
    {
        public int? epoch_no { get; set; }
        public double? voting_power { get; set; }
    }

    public class DrepNewRegisterResponseDto
    {
        public long? block_time { get; set; }
        public string? drep_id { get; set; }
        public string? action { get; set; }
    }

    public class DrepMetadataResponseDto
    {
        public string? drep_id { get; set; }
        public string? hex { get; set; }
        public bool? has_script { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_url { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_hash { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_json { get; set; }
        [Column(TypeName = "jsonb")]
        public string? bytes { get; set; }
        [Column(TypeName = "jsonb")]
        public string? warning { get; set; }
        [Column(TypeName = "jsonb")]
        public string? language { get; set; }
        [Column(TypeName = "jsonb")]
        public string? comment { get; set; }
        [Column(TypeName = "jsonb")]
        public string? is_valid { get; set; }
    }

    public class TotalWalletStatisticsResponseDto
    {
        public List<WalletDrepDto>? delegators { get; set; }
        public List<WalletLiveDelegatorDto>? live_delegators { get; set; }
        public List<WalletAmountDto>? amounts { get; set; }
    }

    public class WalletDrepDto
    {
        public int epoch_no { get; set; }
        public double delegator { get; set; }
    }

    public class WalletLiveDelegatorDto
    {
        public int epoch_no { get; set; }
        public double live_delegators { get; set; }
    }

    public class WalletAmountDto
    {
        public int epoch_no { get; set; }
        public double amount { get; set; }
    }

    public class DrepsUpdatesResponseDto
    {
        public string? drep_id { get; set; }
        public string? hex { get; set; }
        public bool? has_script { get; set; }
        public string? update_tx_hash { get; set; }
        public int? cert_index { get; set; }
        public long? block_time { get; set; }
        public string? action { get; set; }
        public string? deposit { get; set; }
        public string? meta_url { get; set; }
        public string? meta_hash { get; set; }
        public string? meta_json { get; set; }
    }

    // DTOs for Koios API responses
    public class KoiosDrepDelegatorsApiResponse
    {
        public string? stake_address { get; set; }
        public string? stake_address_hex { get; set; }
        public string? script_hash { get; set; }
        public int? epoch_no { get; set; }
        public string? amount { get; set; }
    }

    public class KoiosAccountUpdatesApiResponse
    {
        public string? stake_address { get; set; }
        public List<KoiosAccountUpdateItem>? updates { get; set; }
    }

    public class KoiosAccountUpdateItem
    {
        public string? action_type { get; set; }
        public int? epoch_no { get; set; }
        public long? block_time { get; set; }
    }

    public class KoiosAccountUpdatesRequest
    {
        public List<string>? _stake_addresses { get; set; }
    }

    // Additional DTOs needed for service implementations
    public class DrepCardInfoDto
    {
        public int? total_registered { get; set; }
        public int? total_delegated { get; set; }
        public double? total_voting_power { get; set; }
        public int? total_governance_actions { get; set; }
    }

    public class DrepDetailsResponseDto
    {
        public string? drep_id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public double? voting_power { get; set; }
        public int? delegators { get; set; }
        public string? status { get; set; }
        public string? image { get; set; }
        public bool? active { get; set; }
    }

    public class DrepVotingPowerResponseDto
    {
        public string? drep_id { get; set; }
        public int? epoch_no { get; set; }
        public double? voting_power { get; set; }
        public double? percentage { get; set; }
    }
}