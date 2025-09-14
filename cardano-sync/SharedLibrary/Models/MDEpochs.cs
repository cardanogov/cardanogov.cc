using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDEpochs
    {
        public int? no { get; set; }
        public string? tx_amount { get; set; }
        public string? circulating_supply { get; set; }
        public int? pool { get; set; }
        public int? pool_with_block { get; set; }
        public int? pool_with_stake { get; set; }
        public string? pool_fee { get; set; }
        public string? reward_amount { get; set; }
        public string? stake { get; set; }
        public int? delegator { get; set; }
        public int? account { get; set; }
        public int? account_with_reward { get; set; }
        public int? pool_register { get; set; }
        public int? pool_retire { get; set; }
        public string? orphaned_reward_amount { get; set; }
        public int? block_with_tx { get; set; }
        public int? byron { get; set; }
        public int? byron_with_amount { get; set; }
        public string? byron_amount { get; set; }
        public int? account_with_amount { get; set; }
        public int? delegator_with_stake { get; set; }
        public int? token { get; set; }
        public int? token_policy { get; set; }
        public int? token_holder { get; set; }
        public int? token_tx { get; set; }
        public string? out_sum { get; set; }
        public string? fees { get; set; }
        public int? tx { get; set; }
        public int? block { get; set; }
        public long? start_time { get; set; }
        public long? end_time { get; set; }
        public int? optimal_pool_count { get; set; }
        public double? decentralisation { get; set; }
        public string? nonce { get; set; }
        [Column(TypeName = "jsonb")]
        public string? holder_range { get; set; }
        public double? exchange_rate { get; set; }
    }
}