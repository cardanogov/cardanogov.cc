namespace SharedLibrary.DTOs
{
    public class EpochDto
    {
        public int? epoch_no { get; set; }
        public string? amount { get; set; }
        public string? dreps { get; set; }
    }

    public class EpochInfoDto
    {
        public int? epoch_no { get; set; }
        public string? out_sum { get; set; }
        public string? fees { get; set; }
        public int? tx_count { get; set; }
        public int? blk_count { get; set; }
        public int? start_time { get; set; }
        public int? end_time { get; set; }
        public int? first_block_time { get; set; }
        public int? last_block_time { get; set; }
        public string? active_stake { get; set; }
        public string? total_rewards { get; set; }
        public string? avg_blk_reward { get; set; }
    }

    // Response DTOs for API endpoints
    public class EpochInfoResponseDto
    {
        public int? epoch_no { get; set; }
        public string? out_sum { get; set; }
        public string? fees { get; set; }
        public int? tx_count { get; set; }
        public int? blk_count { get; set; }
        public int? start_time { get; set; }
        public int? end_time { get; set; }
        public int? first_block_time { get; set; }
        public int? last_block_time { get; set; }
        public string? active_stake { get; set; }
        public string? total_rewards { get; set; }
        public string? avg_blk_reward { get; set; }
    }

    public class CurrentEpochResponseDto
    {
        public int? epoch_no { get; set; }
        public string? amount { get; set; }
        public int? dreps { get; set; }
    }

    public class EpochInfoSpoResponseDto
    {
        public double? active_stake { get; set; }
    }

    public class EpochProtocolParametersResponseDto
    {
        public int? epoch_no { get; set; }
        public int? min_fee_a { get; set; }
        public int? min_fee_b { get; set; }
        public int? max_block_size { get; set; }
        public int? max_tx_size { get; set; }
        public int? max_block_header_size { get; set; }
        public string? key_deposit { get; set; }
        public string? pool_deposit { get; set; }
        public int? max_epoch { get; set; }
        public int? optimal_pool_count { get; set; }
        public double? influence { get; set; }
        public double? monetary_expand_rate { get; set; }
        public double? treasury_growth_rate { get; set; }
        public double? decentralisation { get; set; }
        public string? entropy { get; set; }
        public int? protocol_major { get; set; }
        public int? protocol_minor { get; set; }
        public string? min_utxo { get; set; }
        public string? min_pool_cost { get; set; }
        public string? nonce { get; set; }
        public double? price_mem { get; set; }
        public double? price_step { get; set; }
        public string? max_tx_ex_mem { get; set; }
        public string? max_tx_ex_steps { get; set; }
        public string? max_block_ex_mem { get; set; }
        public string? max_block_ex_steps { get; set; }
        public int? max_val_size { get; set; }
        public int? collateral_percent { get; set; }
        public int? max_collateral_inputs { get; set; }
        public string? coins_per_utxo_size { get; set; }
    }
}