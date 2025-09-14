namespace MainAPI.Application.Queries.Drep
{
    public class GetDrepInfoQuery : IRequest<DrepInfoResponseDto?>
    {
        public string DrepId { get; }

        public GetDrepInfoQuery(string drepId)
        {
            DrepId = drepId;
        }
    }

    public class GetDrepInfoQueryHandler : IRequestHandler<GetDrepInfoQuery, DrepInfoResponseDto?>
    {
        private readonly IDrepService _drepService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetDrepInfoQueryHandler> _logger;

        public GetDrepInfoQueryHandler(
            IDrepService drepService,
            ICacheService cacheService,
            ILogger<GetDrepInfoQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<DrepInfoResponseDto?> Handle(GetDrepInfoQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = $"{CacheKeys.DREP_INFO}_{request.DrepId}";
                _logger.LogInformation("Processing GetDrepInfoQuery for DrepId: {DrepId} with cache key: {CacheKey}", request.DrepId, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching drep info data for DrepId: {DrepId}", request.DrepId);
                        var data = await _drepService.GetDrepInfoAsync(request.DrepId);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GetDrepInfoQuery for DrepId: {DrepId}", request.DrepId);
                throw;
            }
        }
    }
}