using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDDrepsDelegators
    {
        public string? drep_id { get; set; }
        public string? stake_address { get; set; }
        public string? stake_address_hex { get; set; }
        [Column(TypeName = "jsonb")]
        public string? script_hash { get; set; }
        public int? epoch_no { get; set; }
        public string? amount { get; set; }
    }
}