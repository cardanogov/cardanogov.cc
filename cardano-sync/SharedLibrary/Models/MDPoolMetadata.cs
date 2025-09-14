using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDPoolMetadata
    {
        public string? pool_id_bech32 { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_url { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_hash { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_json { get; set; }
    }
}
