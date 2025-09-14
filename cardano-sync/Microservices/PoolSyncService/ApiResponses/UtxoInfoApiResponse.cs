using System.Text.Json.Serialization;

namespace PoolSyncService.ApiResponses
{
    /// <summary>
    /// API Response model for utxo_info endpoint
    /// Maps to MDUtxoInfo database model
    /// </summary>
    public class UtxoInfoApiResponse
    {
        /// <summary>
        /// Transaction hash
        /// </summary>
        [JsonPropertyName("tx_hash")]
        public string? tx_hash { get; set; }

        /// <summary>
        /// Transaction output index
        /// </summary>
        [JsonPropertyName("tx_index")]
        public short? tx_index { get; set; }

        /// <summary>
        /// Address that owns this UTXO
        /// </summary>
        [JsonPropertyName("address")]
        public string? address { get; set; }

        /// <summary>
        /// Value of the UTXO in lovelace
        /// </summary>
        [JsonPropertyName("value")]
        public string? value { get; set; }

        /// <summary>
        /// Stake address associated with this UTXO
        /// </summary>
        [JsonPropertyName("stake_address")]
        public string? stake_address { get; set; }

        /// <summary>
        /// Payment credential
        /// </summary>
        [JsonPropertyName("payment_cred")]
        public string? payment_cred { get; set; }

        /// <summary>
        /// Epoch number when this UTXO was created
        /// </summary>
        [JsonPropertyName("epoch_no")]
        public int? epoch_no { get; set; }

        /// <summary>
        /// Block height
        /// </summary>
        [JsonPropertyName("block_height")]
        public int? block_height { get; set; }

        /// <summary>
        /// Block time as Unix timestamp
        /// </summary>
        [JsonPropertyName("block_time")]
        public int? block_time { get; set; }

        /// <summary>
        /// Hash of the datum if present
        /// </summary>
        [JsonPropertyName("datum_hash")]
        public string? datum_hash { get; set; }

        /// <summary>
        /// Inline datum if present
        /// </summary>
        [JsonPropertyName("inline_datum")]
        public string? inline_datum { get; set; }

        /// <summary>
        /// Reference script if present
        /// </summary>
        [JsonPropertyName("reference_script")]
        public string? reference_script { get; set; }

        /// <summary>
        /// List of assets in this UTXO
        /// </summary>
        [JsonPropertyName("asset_list")]
        public string? asset_list { get; set; }

        /// <summary>
        /// Whether this UTXO has been spent
        /// </summary>
        [JsonPropertyName("is_spent")]
        public bool? is_spent { get; set; }
    }
}