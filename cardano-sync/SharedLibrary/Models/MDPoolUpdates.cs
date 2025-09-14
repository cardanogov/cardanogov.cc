using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDPoolUpdates
    {
        public string? tx_hash { get; set; }
        public long? block_time { get; set; }
        public string? pool_id_bech32 { get; set; }
        public string? pool_id_hex { get; set; }
        public string? active_epoch_no { get; set; }
        public string? vrf_key_hash { get; set; }
        public string? margin { get; set; }
        public string? fixed_cost { get; set; }
        public string? pledge { get; set; }
        public string? reward_addr { get; set; }
        [Column(TypeName = "jsonb")]
        public string? owners { get; set; }
        [Column(TypeName = "jsonb")]
        public string? relays { get; set; }
        public string? meta_url { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_hash { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_json { get; set; }
        public string? update_type { get; set; }
        public string? retiring_epoch { get; set; }
    }
}