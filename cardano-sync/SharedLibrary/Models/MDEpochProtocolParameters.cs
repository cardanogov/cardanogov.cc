using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDEpochProtocolParameters
    {
        public int? epoch_no { get; set; }

        public string? min_fee_a { get; set; }

        public string? min_fee_b { get; set; }

        public string? max_block_size { get; set; }

        public string? max_tx_size { get; set; }

        public string? max_bh_size { get; set; }

        public string? key_deposit { get; set; }

        public string? pool_deposit { get; set; }

        public string? max_epoch { get; set; }

        public string? optimal_pool_count { get; set; }

        public string? influence { get; set; }

        public string? monetary_expand_rate { get; set; }

        public string? treasury_growth_rate { get; set; }

        public string? decentralisation { get; set; }

        public string? extra_entropy { get; set; }

        public string? protocol_major { get; set; }

        public string? protocol_minor { get; set; }

        public string? min_utxo_value { get; set; }

        public string? min_pool_cost { get; set; }

        public string? nonce { get; set; }
        public string? block_hash { get; set; }
        [Column(TypeName = "jsonb")]
        public string? cost_models { get; set; }

        public string? price_mem { get; set; }

        public string? price_step { get; set; }

        public string? max_tx_ex_mem { get; set; }

        public string? max_tx_ex_steps { get; set; }

        public string? max_block_ex_mem { get; set; }

        public string? max_block_ex_steps { get; set; }

        public string? max_val_size { get; set; }

        public string? collateral_percent { get; set; }

        public string? max_collateral_inputs { get; set; }

        public string? coins_per_utxo_size { get; set; }

        public string? pvt_motion_no_confidence { get; set; }

        public string? pvt_committee_normal { get; set; }

        public string? pvt_committee_no_confidence { get; set; }

        public string? pvt_hard_fork_initiation { get; set; }

        public string? dvt_motion_no_confidence { get; set; }

        public string? dvt_committee_normal { get; set; }

        public string? dvt_committee_no_confidence { get; set; }

        public string? dvt_update_to_constitution { get; set; }

        public string? dvt_hard_fork_initiation { get; set; }

        public string? dvt_p_p_network_group { get; set; }

        public string? dvt_p_p_economic_group { get; set; }

        public string? dvt_p_p_technical_group { get; set; }

        public string? dvt_p_p_gov_group { get; set; }

        public string? dvt_treasury_withdrawal { get; set; }

        public string? committee_min_size { get; set; }

        public string? committee_max_term_length { get; set; }

        public string? gov_action_lifetime { get; set; }

        public string? gov_action_deposit { get; set; }

        public string? drep_deposit { get; set; }

        public string? drep_activity { get; set; }

        public string? pvtpp_security_group { get; set; }

        public string? min_fee_ref_script_cost_per_byte { get; set; }
    }
}