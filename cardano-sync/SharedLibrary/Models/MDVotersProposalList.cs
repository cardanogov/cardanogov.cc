using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDVotersProposalList
    {
        public long? block_time { get; set; }
        public string? proposal_id { get; set; }
        public string? proposal_tx_hash { get; set; }
        public int? proposal_index { get; set; }
        public string? proposal_type { get; set; }
        [Column(TypeName = "jsonb")]
        public string? proposal_description { get; set; }
        [Column(TypeName = "jsonb")]
        public string? deposit { get; set; }
        public string? return_address { get; set; }
        public int? proposed_epoch { get; set; }
        [Column(TypeName = "jsonb")]
        public string? ratified_epoch { get; set; }
        [Column(TypeName = "jsonb")]
        public string? enacted_epoch { get; set; }
        [Column(TypeName = "jsonb")]
        public string? dropped_epoch { get; set; }
        [Column(TypeName = "jsonb")]
        public string? expired_epoch { get; set; }
        [Column(TypeName = "jsonb")]
        public string? expiration { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_url { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_hash { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_json { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_comment { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_language { get; set; }
        [Column(TypeName = "jsonb")]
        public string? meta_is_valid { get; set; }
        [Column(TypeName = "jsonb")]
        public string? withdrawal { get; set; }
        [Column(TypeName = "jsonb")]
        public string? param_proposal { get; set; }
    }
}