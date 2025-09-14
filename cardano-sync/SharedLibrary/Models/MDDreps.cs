namespace SharedLibrary.Models
{
    public class MDDreps
    {
        public string? hash { get; set; }
        public string? bech32_legacy { get; set; }
        public bool? has_script { get; set; }
        public string? tx_hash { get; set; }
        public string? url { get; set; }
        public string? comment { get; set; }
        public string? payment_address { get; set; }
        public string? given_name { get; set; }
        public string? objectives { get; set; }
        public string? motivations { get; set; }
        public string? qualifications { get; set; }
        //public string? image { get; set; }
        public string? live_stake { get; set; }
        public int? delegator { get; set; }
        public long? tx_time { get; set; }
        public int? last_active_epoch { get; set; }
        public string? bech32 { get; set; }
    }
}