using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDCommitteeInformation
    {
        public string? proposal_id { get; set; }
        public string? proposal_tx_hash { get; set; }
        public int? proposal_index { get; set; }
        public int? quorum_numerator { get; set; }
        public int? quorum_denominator { get; set; }
        [Column(TypeName = "jsonb")]
        public string? members { get; set; }
    }
}