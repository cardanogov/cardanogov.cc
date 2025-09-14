namespace PoolSyncService.ApiResponses
{
    public class PoolUpdatesApiResponse
    {
        public string? tx_hash { get; set; }
        public int? block_time { get; set; }
        public string? pool_id_bech32 { get; set; }
        public string? pool_id_hex { get; set; }
        public string? active_epoch_no { get; set; }
        public string? vrf_key_hash { get; set; }
        public double? margin { get; set; }
        public string? fixed_cost { get; set; }
        public string? pledge { get; set; }
        public string? reward_addr { get; set; }
        public List<string>? owners { get; set; }
        public List<PoolRelayResponse>? relays { get; set; }
        public string? meta_url { get; set; }
        public string? meta_hash { get; set; }
        public PoolMetaJsonResponse? meta_json { get; set; }
        public string? update_type { get; set; }
        public int? retiring_epoch { get; set; }
    }

    public class PoolMetaJsonResponse
    {
        public string? name { get; set; }
        public string? ticker { get; set; }
        public string? homepage { get; set; }
        public string? description { get; set; }
    }
}