using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDUtxoInfo
    {
        public string? tx_hash { get; set; }
        public int? tx_index { get; set; }
        [Column(TypeName = "jsonb")]
        public string? stake_address { get; set; }
        public int? epoch_no { get; set; }
        public long? block_time { get; set; }
    }
}