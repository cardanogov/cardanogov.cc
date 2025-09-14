using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDProposalVotes
    {
        public long? block_time { get; set; }
        public string? voter_role { get; set; }
        public string? voter_id { get; set; }
        public string? voter_hex { get; set; }
        public bool? voter_has_script { get; set; }
        public string? vote { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_url { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_hash { get; set; }
        public string? proposal_id { get; set; }
    }
}