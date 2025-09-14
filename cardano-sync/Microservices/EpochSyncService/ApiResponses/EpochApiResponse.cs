namespace EpochSyncService.ApiResponses;

// DTO class for API response
public class EpochApiResponse
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
    public string? active_stake { get; set; }
    public string? total_rewards { get; set; }
    public string? avg_blk_reward { get; set; }
}