using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    [Table("md_generated_images")]
    public class GeneratedImage
    {
        public string Text { get; set; } = string.Empty;

        [Column("subtext")]
        public string Subtext { get; set; } = string.Empty;

        [Column("image_url")]
        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        [Column("cache_key")]
        [Key]
        public string CacheKey { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}