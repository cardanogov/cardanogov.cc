using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDPoolStakeSnapshot
    {
        public string pool_id_bech32 { get; set; } = string.Empty;
        public string? snapshot { get; set; }
        public int? epoch_no { get; set; }
        [Column(TypeName = "jsonb")]
        public string? nonce { get; set; }
        public string? pool_stake { get; set; }
        public string? active_stake { get; set; }
    }
}
