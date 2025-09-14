using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDDrepsInfo
    {
        public string? drep_id { get; set; }
        public string? hex { get; set; }
        public bool? has_script { get; set; }
        public bool? registered { get; set; }
        [Column(TypeName = "jsonb")]
        public string? deposit { get; set; }
        public bool? active { get; set; }
        [Column(TypeName = "jsonb")]
        public string? expires_epoch_no { get; set; }
        public string? amount { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_url { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_hash { get; set; }
    }
}