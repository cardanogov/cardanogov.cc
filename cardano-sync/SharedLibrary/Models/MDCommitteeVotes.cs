using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDCommitteeVotes
    {
        public string? proposal_id { get; set; }
        public string? proposal_tx_hash { get; set; }
        public int? proposal_index { get; set; }
        public string? vote_tx_hash { get; set; }
        public long? block_time { get; set; }
        public string? vote { get; set; }

        [Column(TypeName = "jsonb")]
        public string? meta_url { get; set; }

        [Column(TypeName = "jsonb")]
        public string? meta_hash { get; set; }
        public string? cc_hot_id { get; set; }
    }
}