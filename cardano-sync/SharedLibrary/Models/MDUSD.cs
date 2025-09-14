using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDUSD
    {
        [Column(TypeName = "jsonb")]
        public string? cardano { get; set; }
    }
}