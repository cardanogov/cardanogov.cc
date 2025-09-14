using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDAccountList
    {
        public string? stake_address { get; set; }
        public string? stake_address_hex { get; set; }
        [Column(TypeName = "jsonb")]
        public string? script_hash { get; set; }
    }
}