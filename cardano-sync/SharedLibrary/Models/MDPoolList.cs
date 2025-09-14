using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDPoolList
    {
        public string? pool_id_bech32 { get; set; }
        public string? pool_id_hex { get; set; }
        public string? active_epoch_no { get; set; }
        [Column(TypeName = "jsonb")]
        public string? margin { get; set; }
        [Column(TypeName = "jsonb")]
        public string? fixed_cost { get; set; }
        [Column(TypeName = "jsonb")]
        public string? pledge { get; set; }
        [Column(TypeName = "jsonb")]
        public string? deposit { get; set; }
        [Column(TypeName = "jsonb")]
        public string? reward_addr { get; set; }
        [Column(TypeName = "jsonb")]
        public string? owners { get; set; }
        [Column(TypeName = "jsonb")]
        public string? relays { get; set; }
        public string? ticker { get; set; }
        public string? pool_group { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_url { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_hash { get; set; }
        public string? pool_status { get; set; }
        public string? active_stake { get; set; }
        [Column(TypeName = "jsonb")]
        public string? retiring_epoch { get; set; }
    }
}
