using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDDrepsUpdates
    {
        public string? drep_id { get; set; }
        public string? hex { get; set; }
        public bool? has_script { get; set; }
        public string? update_tx_hash { get; set; }
        public int? cert_index { get; set; }
        public long? block_time { get; set; }
        public string? action { get; set; }
        [Column(TypeName = "jsonb")]
        public string? deposit { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_url { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_hash { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_json { get; set; }
    }
}