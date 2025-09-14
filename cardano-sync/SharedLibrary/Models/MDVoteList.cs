using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDVoteList
    {
        public string? vote_tx_hash { get; set; }
        public string? voter_role { get; set; }
        public string? voter_id { get; set; }
        public string? proposal_id { get; set; }
        public string? proposal_tx_hash { get; set; }
        public int? proposal_index { get; set; }
        public string? proposal_type { get; set; }
        public int? epoch_no { get; set; }
        public int? block_height { get; set; }
        public long? block_time { get; set; }
        public string? vote { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_url { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_hash { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_json { get; set; }
    }
}