using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class ApiKey
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public ApiKeyType Type { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Column(TypeName = "timestamp")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "timestamp")]
        public DateTime? ExpiresAt { get; set; }

        [Column(TypeName = "timestamp")]
        public DateTime? LastUsedAt { get; set; }

        public int TotalRequests { get; set; } = 0;

        public int DailyRequests { get; set; } = 0;

        [Column(TypeName = "timestamp")]
        public DateTime? LastDailyReset { get; set; }

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        [StringLength(500)]
        public string? AllowedOrigins { get; set; }

        [StringLength(500)]
        public string? AllowedEndpoints { get; set; }
    }

    public enum ApiKeyType
    {
        Free = 0,
        Premium = 1,
        Enterprise = 2
    }
}