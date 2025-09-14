namespace MainAPI.Application.Queries.Committee
{
    public class GetTotalCommitteeQuery : IRequest<int?>
    {
    }

    public class GetTotalCommitteeQueryHandler : IRequestHandler<GetTotalCommitteeQuery, int?>
    {
        private readonly ICommitteeService _committeeService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetTotalCommitteeQueryHandler> _logger;

        public GetTotalCommitteeQueryHandler(
            ICommitteeService committeeService,
            ICacheService cacheService,
            ILogger<GetTotalCommitteeQueryHandler> logger)
        {
            _committeeService = committeeService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<int?> Handle(GetTotalCommitteeQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.TOTAL_COMMITTEE;

            try
            {
                _logger.LogInformation("Processing GetTotalCommitteeQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching total committee data");
                        var data = await _committeeService.GetTotalCommitteeAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total committee data");
                throw;
            }
        }
    }
}