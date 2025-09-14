namespace DrepSyncService.ApiResponses
{
    /// <summary>
    /// API Response model for /drep_updates endpoint
    /// Maps to MDDrepsUpdates database model
    /// </summary>
    public class DrepUpdatesApiResponse
    {
        /// <summary>
        /// DRep ID in Bech32 format
        /// </summary>
        public string drep_id { get; set; } = "";

        /// <summary>
        /// DRep ID in hex format
        /// </summary>
        public string hex { get; set; } = "";

        /// <summary>
        /// Whether the DRep has a script
        /// </summary>
        public bool has_script { get; set; }

        /// <summary>
        /// Transaction hash of the update
        /// </summary>
        public string update_tx_hash { get; set; } = "";

        /// <summary>
        /// Certificate index in the transaction
        /// </summary>
        public int cert_index { get; set; }

        /// <summary>
        /// Block time (Unix timestamp)
        /// </summary>
        public long block_time { get; set; }

        /// <summary>
        /// Action type (registered, deregistered, etc.)
        /// </summary>
        public string action { get; set; } = "";

        /// <summary>
        /// Deposit amount in lovelace
        /// </summary>
        public string deposit { get; set; } = "";

        /// <summary>
        /// Metadata URL
        /// </summary>
        public string? meta_url { get; set; }

        /// <summary>
        /// Metadata hash
        /// </summary>
        public string? meta_hash { get; set; }

        /// <summary>
        /// Metadata JSON content
        /// </summary>
        public string? meta_json { get; set; }
    }
}
