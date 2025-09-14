namespace MainAPI.Application.Queries.Pool
{
    public class GetTotalsQuery : IRequest<List<TotalInfoResponseDto>?>
    {
        public int EpochNo { get; }

        public GetTotalsQuery(int epochNo)
        {
            EpochNo = epochNo;
        }
    }

    public class GetTotalsQueryHandler : IRequestHandler<GetTotalsQuery, List<TotalInfoResponseDto>?>
    {
        private readonly IPoolService _poolService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetTotalsQueryHandler> _logger;

        public GetTotalsQueryHandler(
            IPoolService poolService,
            ICacheService cacheService,
            ILogger<GetTotalsQueryHandler> logger)
        {
            _poolService = poolService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<TotalInfoResponseDto>?> Handle(GetTotalsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.TOTALS}_{request.EpochNo}";

            try
            {
                _logger.LogInformation("Processing GetTotalsQuery for EpochNo: {EpochNo} with cache key: {CacheKey}", request.EpochNo, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching totals data for EpochNo: {EpochNo}", request.EpochNo);
                        var data = await _poolService.GetTotalsAsync(request.EpochNo);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving totals data for epoch {EpochNo}", request.EpochNo);
                throw;
            }
        }
    }
}