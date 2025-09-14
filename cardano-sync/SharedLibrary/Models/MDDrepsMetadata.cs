using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDDrepsMetadata
    {
        public string? drep_id { get; set; }
        public string? hex { get; set; }
        public bool? has_script { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_url { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_hash { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_json { get; set; }
        [Column(TypeName = "jsonb")]
        public string? bytes { get; set; }
        [Column(TypeName = "jsonb")]
        public string? warning { get; set; }
        [Column(TypeName = "jsonb")]
        public string? language { get; set; }
        [Column(TypeName = "jsonb")]
        public string? comment { get; set; }
        [Column(TypeName = "jsonb")]
        public string? is_valid { get; set; }
    }
}