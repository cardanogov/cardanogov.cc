using System.Text.Json.Serialization;

namespace ProposalSyncService.ApiResponses
{
    /// <summary>
    /// API Response model for proposal_votes endpoint
    /// Maps to MDProposalVotes database model
    /// </summary>
    public class ProposalVotesApiResponse
    {
        /// <summary>
        /// Block time when the vote was made
        /// </summary>
        [JsonPropertyName("block_time")]
        public int? block_time { get; set; }

        /// <summary>
        /// Role of the voter (DRep, CC Member, SPO)
        /// </summary>
        [JsonPropertyName("voter_role")]
        public string? voter_role { get; set; }

        /// <summary>
        /// Voter ID in bech32 format
        /// </summary>
        [JsonPropertyName("voter_id")]
        public string? voter_id { get; set; }

        /// <summary>
        /// Voter ID in hex format
        /// </summary>
        [JsonPropertyName("voter_hex")]
        public string? voter_hex { get; set; }

        /// <summary>
        /// Whether the voter has a script
        /// </summary>
        [JsonPropertyName("voter_has_script")]
        public bool? voter_has_script { get; set; }

        /// <summary>
        /// Vote choice (Yes, No, Abstain)
        /// </summary>
        [JsonPropertyName("vote")]
        public string? vote { get; set; }

        /// <summary>
        /// URL to vote metadata
        /// </summary>
        [JsonPropertyName("meta_url")]
        public string? meta_url { get; set; }

        /// <summary>
        /// Hash of vote metadata
        /// </summary>
        [JsonPropertyName("meta_hash")]
        public string? meta_hash { get; set; }
    }
}