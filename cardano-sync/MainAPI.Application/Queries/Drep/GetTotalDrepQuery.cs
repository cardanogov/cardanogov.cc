namespace MainAPI.Application.Queries.Drep
{
    public class GetTotalDrepQuery : IRequest<int?>
    {
    }

    public class GetTotalDrepQueryHandler : IRequestHandler<GetTotalDrepQuery, int?>
    {
        private readonly IDrepService _drepService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetTotalDrepQueryHandler> _logger;

        public GetTotalDrepQueryHandler(
            IDrepService drepService,
            ICacheService cacheService,
            ILogger<GetTotalDrepQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<int?> Handle(GetTotalDrepQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.TOTAL_DREP;

            try
            {
                _logger.LogInformation("Processing GetTotalDrepQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching total drep data");
                        var data = await _drepService.GetTotalDrepAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total drep data");
                throw;
            }
        }
    }
}