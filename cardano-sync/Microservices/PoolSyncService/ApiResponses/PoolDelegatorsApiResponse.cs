namespace PoolSyncService.ApiResponses
{
    public class PoolDelegatorsApiResponse
    {
        public string? stake_address { get; set; }
        public string? amount { get; set; }
        public long? active_epoch_no { get; set; }
        public string? latest_delegation_tx_hash { get; set; }
    }
}