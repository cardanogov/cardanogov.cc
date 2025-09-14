using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class MDEpoch
    {
        public int? epoch_no { get; set; }
        public string? out_sum { get; set; }
        public string? fees { get; set; }
        public int? tx_count { get; set; }
        public int? blk_count { get; set; }
        public long? start_time { get; set; }
        public long? end_time { get; set; }
        public long? first_block_time { get; set; }
        public long? last_block_time { get; set; }

        [Column(TypeName = "jsonb")]
        public string? active_stake { get; set; }

        [Column(TypeName = "jsonb")]
        public string? total_rewards { get; set; }

        [Column(TypeName = "jsonb")]
        public string? avg_blk_reward { get; set; }
    }
}
