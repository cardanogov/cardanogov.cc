namespace SharedLibrary.Models
{
    public class MDAccountUpdates
    {
        public string? stake_address { get; set; }
        public string? drep_id { get; set; }
        public string? tx_hash { get; set; }
        public int? epoch_no { get; set; }
        public long? block_time { get; set; }
        public int? epoch_slot { get; set; }
        public string? action_type { get; set; }
        public long? absolute_slot { get; set; }
    }
}