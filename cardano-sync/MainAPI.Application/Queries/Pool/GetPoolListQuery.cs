namespace MainAPI.Application.Queries.Pool
{
    public class GetPoolListQuery : IRequest<PoolResponseDto?>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string? Status { get; set; }
        public string? Search { get; set; }

        public GetPoolListQuery(int page, int pageSize, string? status, string? search)
        {
            Page = page;
            PageSize = pageSize;
            Status = status;
            Search = search;
        }
    }

    public class GetPoolListQueryHandler : IRequestHandler<GetPoolListQuery, PoolResponseDto?>
    {
        private readonly IPoolService _poolService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetPoolListQueryHandler> _logger;

        public GetPoolListQueryHandler(
            IPoolService poolService,
            ICacheService cacheService,
            ILogger<GetPoolListQueryHandler> logger)
        {
            _poolService = poolService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<PoolResponseDto?> Handle(GetPoolListQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.POOL_LIST}:{request.Page}:{request.Status ?? ""}:{request.Search ?? ""}";

            try
            {
                _logger.LogInformation("Processing GetPoolListQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching pool list data for Page: {Page}, Search: {Search}, Status: {Status}",
                            request.Page, request.Search, request.Status);
                        var data = await _poolService.GetPoolListAsync(request.Page, request.PageSize, request.Status, request.Search);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GetPoolListQuery with Page: {Page}, Search: {Search}, Status: {Status}",
                    request.Page, request.Search, request.Status);
                throw;
            }
        }
    }
}