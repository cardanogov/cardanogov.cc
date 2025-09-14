using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDDrepsVotingPowerHistory
    {
        public string? drep_id { get; set; }
        public int? epoch_no { get; set; }
        [Column(TypeName = "jsonb")]
        public string? amount { get; set; }
    }
}