using EpochSyncService.ApiResponses;
using System.Text.Json;

namespace EpochSyncService.Services;

public interface IAdastatApiClient
{
    Task<AdastatEpochApiResponse?> GetEpochsAsync(int limit = 1000);
    Task<AdastatEpochApiResponse?> GetEpochsWithCursorAsync(int? after = null, int limit = 1000);
    Task<AdastatDrepsApiResponse?> GetDrepsAsync(int page = 1, int limit = 1000);
}

public class AdastatApiClient : IAdastatApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AdastatApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public AdastatApiClient(HttpClient httpClient, ILogger<AdastatApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    public async Task<AdastatEpochApiResponse?> GetEpochsAsync(int limit = 1000)
    {
        try
        {
            var url = $"https://api.adastat.net/rest/v1/epochs.json?dir=desc&currency=usd&sort=no&limit={limit}&rows=true";

            _logger.LogInformation("📡 Calling Adastat API: {Url}", url);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ Adastat API returned error: {StatusCode} - {Reason}",
                    response.StatusCode, response.ReasonPhrase);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AdastatEpochApiResponse>(content, _jsonOptions);

            if (result?.Rows?.Any() == true)
            {
                _logger.LogInformation("✅ Adastat API returned {Count} epochs", result.Rows.Length);
            }
            else
            {
                _logger.LogWarning("⚠️ Adastat API returned no epochs");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error calling Adastat API: {Message}", ex.Message);
            return null;
        }
    }

    public async Task<AdastatEpochApiResponse?> GetEpochsWithCursorAsync(int? after = null, int limit = 1000)
    {
        try
        {
            var url = $"https://api.adastat.net/rest/v1/epochs.json?dir=desc&currency=usd&sort=no&limit={limit}&rows=true";

            if (after.HasValue)
            {
                url += $"&after={after.Value}";
            }

            _logger.LogInformation("📡 Calling Adastat API with cursor: {Url}", url);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ Adastat API returned error: {StatusCode} - {Reason}",
                    response.StatusCode, response.ReasonPhrase);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AdastatEpochApiResponse>(content, _jsonOptions);

            if (result?.Rows?.Any() == true)
            {
                _logger.LogInformation("✅ Adastat API returned {Count} epochs (after: {After})",
                    result.Rows.Length, after);
            }
            else
            {
                _logger.LogWarning("⚠️ Adastat API returned no epochs (after: {After})", after);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error calling Adastat API with cursor: {Message}", ex.Message);
            return null;
        }
    }

    public async Task<AdastatDrepsApiResponse?> GetDrepsAsync(int page = 1, int limit = 1000)
    {
        try
        {
            var url = $"https://adastat.net/api/rest/v1/dreps.json?rows=all&sort=reg_time&dir=desc&limit={limit}&page={page}&currency=usd";

            _logger.LogInformation("📡 Calling Adastat DReps API: {Url}", url);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ Adastat DReps API returned error: {StatusCode} - {Reason}",
                    response.StatusCode, response.ReasonPhrase);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AdastatDrepsApiResponse>(content, _jsonOptions);

            if (result?.Rows?.Any() == true)
            {
                _logger.LogInformation("✅ Adastat DReps API returned {Count} DReps (page: {Page})",
                    result.Rows.Length, page);
            }
            else
            {
                _logger.LogWarning("⚠️ Adastat DReps API returned no DReps (page: {Page})", page);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error calling Adastat DReps API: {Message}", ex.Message);
            return null;
        }
    }
}