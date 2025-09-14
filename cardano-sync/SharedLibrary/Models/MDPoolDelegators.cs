namespace SharedLibrary.Models
{
    public class MDPoolDelegators
    {
        public string pool_id_bech32 { get; set; } = string.Empty;
        public string? stake_address { get; set; }
        public string? amount { get; set; }
        public int? active_epoch_no { get; set; }
        public string? latest_delegation_tx_hash { get; set; }
    }
}