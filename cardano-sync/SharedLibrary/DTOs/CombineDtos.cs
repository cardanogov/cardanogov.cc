namespace SharedLibrary.DTOs
{
    public class MembershipDataResponseDto
    {
        public int? total_stake_addresses { get; set; }
        public int? total_pool { get; set; }
        public int? total_drep { get; set; }
        public int? total_committee { get; set; }
    }

    public class ParticipateInVotingResponseDto
    {
        public List<PoolDataDto>? pool { get; set; }
        public List<DrepDataDto>? drep { get; set; }
        public List<int>? committee { get; set; }
    }

    public class PoolDataDto
    {
        public int? epoch_no { get; set; }
        public int? total { get; set; }
    }

    public class DrepDataDto
    {
        public int? epoch_no { get; set; }
        public int? dreps { get; set; }
    }

    public class EpochStats
    {
        public int registered { get; set; }
    }

    public class GovernanceParametersResponseDto
    {
        public int? epoch_no { get; set; }
        public double? max_block_size { get; set; }
        public double? max_tx_size { get; set; }
        public double? max_bh_size { get; set; }
        public double? max_val_size { get; set; }
        public double? max_tx_ex_mem { get; set; }
        public double? max_tx_ex_steps { get; set; }
        public double? max_block_ex_mem { get; set; }
        public double? max_block_ex_steps { get; set; }
        public double? max_collateral_inputs { get; set; }
        public double? min_fee_a { get; set; }
        public double? min_fee_b { get; set; }
        public double? key_deposit { get; set; }
        public double? pool_deposit { get; set; }
        public double? monetary_expand_rate { get; set; }
        public double? treasury_growth_rate { get; set; }
        public double? min_pool_cost { get; set; }
        public double? coins_per_utxo_size { get; set; }
        public double? price_mem { get; set; }
        public double? price_step { get; set; }
        public double? influence { get; set; }
        public int? max_epoch { get; set; }
        public int? optimal_pool_count { get; set; }
        public Dictionary<string, List<int>>? cost_models { get; set; }
        public double? collateral_percent { get; set; }
        public double? gov_action_lifetime { get; set; }
        public double? gov_action_deposit { get; set; }
        public double? drep_deposit { get; set; }
        public double? drep_activity { get; set; }
        public double? committee_min_size { get; set; }
        public double? committee_max_term_length { get; set; }
    }

    public class AllocationResponseDto
    {
        public double? totalActive { get; set; }
        public double? circulatingSupply { get; set; }
        public double? delegation { get; set; }
        public double? adaStaking { get; set; }
        public double? total { get; set; }
    }

    public class SearchApiResponseDto
    {
        public List<ChartDto>? charts { get; set; }
        public List<ProposalSearchDto>? proposals { get; set; }
        public List<DrepSearchDto>? dreps { get; set; }
        public List<PoolSearchDto>? pools { get; set; }
        public List<CcSearchDto>? ccs { get; set; }
    }

    public class ChartDto
    {
        public string? title { get; set; }
        public string? url { get; set; }
    }

    public class ProposalSearchDto
    {
        public string? id { get; set; }
        public string? title { get; set; }
        public string? url { get; set; }
        public string? type { get; set; }
        public double? yes { get; set; }
        public double? no { get; set; }
        public double? abstain { get; set; }
    }

    public class DrepSearchDto
    {
        public string? id { get; set; }
        public string? title { get; set; }
        public string? url { get; set; }
        public int? delegator { get; set; }
        public double? live_stake { get; set; }
    }

    public class PoolSearchDto
    {
        public string? id { get; set; }
        public string? title { get; set; }
        public string? url { get; set; }
        public double? live_stake { get; set; }
        public double? margin { get; set; }
        public double? fixed_cost { get; set; }
        public double? pledge { get; set; }
    }

    public class CcSearchDto
    {
        public string? id { get; set; }
        public string? title { get; set; }
        public string? url { get; set; }
    }

    public class CombinedCardDataResponseDto
    {
        public int? total_proposals { get; set; }
        public int? total_votes { get; set; }
        public int? active_proposals { get; set; }
        public int? total_governance_actions { get; set; }
    }
}