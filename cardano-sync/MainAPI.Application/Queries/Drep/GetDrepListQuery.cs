namespace MainAPI.Application.Queries.Drep
{
    public class GetDrepListQuery : IRequest<DrepListResponseDto?>
    {
        public int Page { get; }
        public string? Search { get; }
        public string? Status { get; }

        public GetDrepListQuery(int page = 1, string? search = null, string? status = null)
        {
            Page = page;
            Search = search;
            Status = status;
        }
    }

    public class GetDrepListQueryHandler : IRequestHandler<GetDrepListQuery, DrepListResponseDto?>
    {
        private readonly IDrepService _drepService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetDrepListQueryHandler> _logger;

        public GetDrepListQueryHandler(
            IDrepService drepService,
            ICacheService cacheService,
            ILogger<GetDrepListQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<DrepListResponseDto?> Handle(GetDrepListQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = $"{CacheKeys.DREP_LIST}:{request.Page}:{request.Status ?? ""}:{request.Search ?? ""}";
                _logger.LogInformation("Processing GetDrepListQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching drep list data for Page: {Page}, Search: {Search}, Status: {Status}",
                            request.Page, request.Search, request.Status);
                        var data = await _drepService.GetDrepListAsync(request.Page, request.Search, request.Status);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GetDrepListQuery with Page: {Page}, Search: {Search}, Status: {Status}",
                    request.Page, request.Search, request.Status);
                throw;
            }
        }
    }
}