using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDPoolInformation
    {
        public string? pool_id_bech32 { get; set; }
        public string? pool_id_hex { get; set; }
        public int? active_epoch_no { get; set; }
        public string? vrf_key_hash { get; set; }
        public double? margin { get; set; }
        public string? fixed_cost { get; set; }
        public string? pledge { get; set; }
        public string? deposit { get; set; }
        public string? reward_addr { get; set; }

        public string? reward_addr_delegated_drep { get; set; }
        [Column(TypeName = "jsonb")]
        public string? owners { get; set; }
        [Column(TypeName = "jsonb")]
        public string? relays { get; set; }
        public string? meta_url { get; set; }
        public string? meta_hash { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_json { get; set; }
        public string? pool_status { get; set; }
        public int? retiring_epoch { get; set; }
        public string? op_cert { get; set; }
        public int? op_cert_counter { get; set; }
        public string? active_stake { get; set; }
        public double? sigma { get; set; }
        public int? block_count { get; set; }
        public string? live_pledge { get; set; }
        public string? live_stake { get; set; }
        public int? live_delegators { get; set; }
        public double? live_saturation { get; set; }
        public string? voting_power { get; set; }
    }
}