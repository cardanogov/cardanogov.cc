using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDPoolsVotingPowerHistory
    {
        public string? pool_id_bech32 { get; set; }
        public int? epoch_no { get; set; }
        [Column(TypeName = "jsonb")]
        public string? amount { get; set; }
    }
}