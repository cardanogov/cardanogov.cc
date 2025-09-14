namespace DrepSyncService.ApiResponses;

public class DrepInfoApiResponse
{
    public string? drep_id { get; set; }
    public string? hex { get; set; }
    public bool? has_script { get; set; }
    public bool? registered { get; set; }
    public string? deposit { get; set; }
    public bool? active { get; set; }
    public int? expires_epoch_no { get; set; }
    public string? amount { get; set; }
    public string? meta_url { get; set; }
    public string? meta_hash { get; set; }
}
