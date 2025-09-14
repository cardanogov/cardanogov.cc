namespace SharedLibrary.Models
{
    public class MDQueryChainTip
    {
        public string? hash { get; set; }
        public int? epoch_no { get; set; }
        public int? abs_slot { get; set; }
        public int? epoch_slot { get; set; }
        public long? block_time { get; set; }
    }
}