namespace PoolSyncService.ApiResponses
{
    public class PoolStakeSnapshotApiResponse
    {
        public string? snapshot { get; set; }
        public long? epoch_no { get; set; }
        public string? nonce { get; set; }
        public string? pool_stake { get; set; }
        public string? active_stake { get; set; }
    }
}