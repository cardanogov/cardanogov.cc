namespace MainAPI.Application.Queries.StakeAddresses
{
    public class GetTotalStakeAddressesQuery : IRequest<int>
    {
        // No parameters needed - just get total count
    }

    public class GetTotalStakeAddressesQueryHandler : IRequestHandler<GetTotalStakeAddressesQuery, int>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GetTotalStakeAddressesQueryHandler> _logger;
        private readonly ICacheService _cacheService;

        private const string KOIOS_API_URL = "https://api.koios.rest/api/v1/account_list?limit=1&select=stake_address";

        public GetTotalStakeAddressesQueryHandler(
            IHttpClientFactory httpClientFactory,
            ILogger<GetTotalStakeAddressesQueryHandler> logger,
            ICacheService cacheService)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<int> Handle(GetTotalStakeAddressesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Executing GetTotalStakeAddressesQuery");

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    CacheKeys.TOTAL_STAKE_ADDRESSES,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching stake addresses count from Koios API");
                        return await GetTotalStakeAddressesFromKoios(cancellationToken);
                    },
                    TimeUtils.GetSecondsUntilEndOfDay()
                );

                _logger.LogInformation("Retrieved total stake addresses successfully: {Count}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GetTotalStakeAddressesQuery");
                throw;
            }
        }

        private async Task<int> GetTotalStakeAddressesFromKoios(CancellationToken cancellationToken)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient("koios");

                // Set up request with Prefer header for count
                var request = new HttpRequestMessage(HttpMethod.Head, KOIOS_API_URL);
                request.Headers.Add("Prefer", "count=exact");

                _logger.LogInformation("Calling Koios API: {Url}", KOIOS_API_URL);

                var response = await httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException($"Koios API returned {response.StatusCode}: {errorContent}");
                }

                // Get the content-range header safely
                string? contentRange = null;

                // Try to get from response headers first
                if (response.Headers.TryGetValues("Content-Range", out var responseValues))
                {
                    contentRange = responseValues.FirstOrDefault();
                }
                // If not found in response headers, try content headers
                else if (response.Content.Headers.TryGetValues("Content-Range", out var contentValues))
                {
                    contentRange = contentValues.FirstOrDefault();
                }

                if (string.IsNullOrEmpty(contentRange))
                {
                    // Log all available headers for debugging
                    _logger.LogWarning("Content-Range header not found. Available response headers: {Headers}",
                        string.Join(", ", response.Headers.Select(h => h.Key)));
                    _logger.LogWarning("Available content headers: {Headers}",
                        string.Join(", ", response.Content.Headers.Select(h => h.Key)));

                    throw new InvalidOperationException("Content-Range header not found in Koios API response");
                }

                _logger.LogInformation("Received Content-Range header: {ContentRange}", contentRange);

                // Parse Content-Range header: "0-999/5287727"
                var parts = contentRange.Split('/');
                if (parts.Length != 2)
                {
                    throw new InvalidOperationException($"Invalid Content-Range format: {contentRange}");
                }

                if (!int.TryParse(parts[1], out var totalCount))
                {
                    totalCount = 0; // Default to 0 if parsing fails
                }

                _logger.LogInformation("Successfully parsed total stake addresses: {Count}", totalCount);
                return totalCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total stake addresses from Koios API");
                throw new ApplicationException("Failed to get total stake addresses from Koios API", ex);
            }
        }
    }
}