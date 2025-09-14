namespace MainAPI.Application.Queries.Drep
{
    public class GetTotalStakeNumbersQuery : IRequest<TotalDrepResponseDto?>
    {
    }

    public class GetTotalStakeNumbersQueryHandler : IRequestHandler<GetTotalStakeNumbersQuery, TotalDrepResponseDto?>
    {
        private readonly IDrepService _drepService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetTotalStakeNumbersQueryHandler> _logger;

        public GetTotalStakeNumbersQueryHandler(
            IDrepService drepService,
            ICacheService cacheService,
            ILogger<GetTotalStakeNumbersQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<TotalDrepResponseDto?> Handle(GetTotalStakeNumbersQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.TOTAL_STAKE_NUMBERS;

            try
            {
                _logger.LogInformation("Processing GetTotalStakeNumbersQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching total stake numbers data");
                        var data = await _drepService.GetTotalStakeNumbersAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total stake numbers data");
                throw;
            }
        }
    }
}