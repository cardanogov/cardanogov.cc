namespace MainAPI.Application.Queries.Drep
{
    public class GetDrepCardDataQuery : IRequest<DrepCardDataResponseDto?>
    {
    }

    public class GetDrepCardDataQueryHandler : IRequestHandler<GetDrepCardDataQuery, DrepCardDataResponseDto?>
    {
        private readonly IDrepService _drepService;
        private readonly MainAPI.Core.Interfaces.ICacheService _cacheService;
        private readonly Microsoft.Extensions.Logging.ILogger<GetDrepCardDataQueryHandler> _logger;

        public GetDrepCardDataQueryHandler(
            IDrepService drepService,
            MainAPI.Core.Interfaces.ICacheService cacheService,
            Microsoft.Extensions.Logging.ILogger<GetDrepCardDataQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<DrepCardDataResponseDto?> Handle(GetDrepCardDataQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.DREP_CARD_DATA;

            try
            {
                _logger.LogInformation("Processing GetDrepCardDataQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching drep card data");
                        var data = await _drepService.GetDrepCardDataAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drep card data");
                throw;
            }
        }
    }
}