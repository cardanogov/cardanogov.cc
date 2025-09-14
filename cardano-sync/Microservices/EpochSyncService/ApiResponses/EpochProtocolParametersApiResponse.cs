namespace EpochSyncService.ApiResponses;

public class EpochProtocolParametersApiResponse
{
    public int? epoch_no { get; set; }
    public int? min_fee_a { get; set; }
    public int? min_fee_b { get; set; }
    public int? max_block_size { get; set; }
    public int? max_tx_size { get; set; }
    public int? max_bh_size { get; set; }
    public string? key_deposit { get; set; }
    public string? pool_deposit { get; set; }
    public int? max_epoch { get; set; }
    public int? optimal_pool_count { get; set; }
    public double? influence { get; set; }
    public double? monetary_expand_rate { get; set; }
    public double? treasury_growth_rate { get; set; }
    public double? decentralisation { get; set; }
    public string? extra_entropy { get; set; }
    public int? protocol_major { get; set; }
    public int? protocol_minor { get; set; }
    public string? min_utxo_value { get; set; }
    public string? min_pool_cost { get; set; }
    public string? nonce { get; set; }
    public string? block_hash { get; set; }
    public string? cost_models { get; set; }
    public double? price_mem { get; set; }
    public double? price_step { get; set; }
    public decimal? max_tx_ex_mem { get; set; }
    public decimal? max_tx_ex_steps { get; set; }
    public decimal? max_block_ex_mem { get; set; }
    public decimal? max_block_ex_steps { get; set; }
    public decimal? max_val_size { get; set; }
    public int? collateral_percent { get; set; }
    public int? max_collateral_inputs { get; set; }
    public string? coins_per_utxo_size { get; set; }

    // Governance fields - Protocol Parameter Voting Thresholds
    public double? pvt_motion_no_confidence { get; set; }
    public double? pvt_committee_normal { get; set; }
    public double? pvt_committee_no_confidence { get; set; }
    public double? pvt_hard_fork_initiation { get; set; }

    // Governance fields - DRep Voting Thresholds
    public double? dvt_motion_no_confidence { get; set; }
    public double? dvt_committee_normal { get; set; }
    public double? dvt_committee_no_confidence { get; set; }
    public double? dvt_update_to_constitution { get; set; }
    public double? dvt_hard_fork_initiation { get; set; }
    public double? dvt_p_p_network_group { get; set; }
    public double? dvt_p_p_economic_group { get; set; }
    public double? dvt_p_p_technical_group { get; set; }
    public double? dvt_p_p_gov_group { get; set; }
    public double? dvt_treasury_withdrawal { get; set; }

    // Governance fields - Committee and Governance Action parameters
    public decimal? committee_min_size { get; set; }
    public decimal? committee_max_term_length { get; set; }
    public decimal? gov_action_lifetime { get; set; }
    public string? gov_action_deposit { get; set; }
    public string? drep_deposit { get; set; }
    public decimal? drep_activity { get; set; }

    // Additional governance fields
    public double? pvtpp_security_group { get; set; }
    public double? min_fee_ref_script_cost_per_byte { get; set; }
}