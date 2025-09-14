namespace SharedLibrary.DTOs
{
    public class CommitteeVotesResponseDto
    {
        public string? proposal_id { get; set; }
        public int? proposal_tx_hash { get; set; }
        public int? proposal_index { get; set; }
        public string? vote_tx_hash { get; set; }
        public long? block_time { get; set; }
        public string? vote { get; set; }
        public string? meta_url { get; set; }
        public string? meta_hash { get; set; }
        public string? cc_hot_id { get; set; }
    }

    public class CommitteeInfoResponseDto
    {
        public string? proposal_id { get; set; }
        public string? proposal_tx_hash { get; set; }
        public int? proposal_index { get; set; }
        public int? quorum_numerator { get; set; }
        public int? quorum_denominator { get; set; }
        public List<MemberDto>? members { get; set; }
    }

    public class MemberDto
    {
        public string? status { get; set; }
        public string? cc_hot_id { get; set; }
        public string? cc_cold_id { get; set; }
        public string? cc_hot_hex { get; set; }
        public string? cc_cold_hex { get; set; }
        public int? expiration_epoch { get; set; }
        public bool? cc_hot_has_script { get; set; }
        public bool? cc_cold_has_script { get; set; }
    }
}