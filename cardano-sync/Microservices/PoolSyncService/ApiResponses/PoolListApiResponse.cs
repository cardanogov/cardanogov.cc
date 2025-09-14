namespace PoolSyncService.ApiResponses
{
    public class PoolListApiResponse
    {
        public string? pool_id_bech32 { get; set; }
        public string? pool_id_hex { get; set; }
        public int? active_epoch_no { get; set; }
        public double? margin { get; set; }
        public string? fixed_cost { get; set; }
        public string? pledge { get; set; }
        public string? deposit { get; set; }
        public string? reward_addr { get; set; }
        public List<string>? owners { get; set; }
        public List<PoolRelayResponse>? relays { get; set; }
        public string? ticker { get; set; }
        public string? pool_group { get; set; }
        public string? meta_url { get; set; }
        public string? meta_hash { get; set; }
        public string? pool_status { get; set; }
        public string? active_stake { get; set; }
        public int? retiring_epoch { get; set; }
    }

    public class PoolRelayResponse
    {
        public string? dns { get; set; }
        public string? srv { get; set; }
        public string? ipv4 { get; set; }
        public string? ipv6 { get; set; }
        public int? port { get; set; }
    }
}