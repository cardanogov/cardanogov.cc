namespace MainAPI.Application.Queries.Combine
{
    public class GetSearchQuery : IRequest<SearchApiResponseDto>
    {
        public string? SearchTerm { get; set; }

        public GetSearchQuery(string? searchTerm = null)
        {
            SearchTerm = searchTerm;
        }
    }

    public class GetSearchQueryHandler : IRequestHandler<GetSearchQuery, SearchApiResponseDto>
    {
        private readonly ICombineService _combineService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetSearchQueryHandler> _logger;

        public GetSearchQueryHandler(
            ICombineService combineService,
            ICacheService cacheService,
            ILogger<GetSearchQueryHandler> logger)
        {
            _combineService = combineService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<SearchApiResponseDto> Handle(GetSearchQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"search_{request.SearchTerm ?? ""}";

            try
            {
                _logger.LogInformation("Processing GetSearchQuery for SearchTerm: {SearchTerm} with cache key: {CacheKey}", request.SearchTerm, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching search data for SearchTerm: {SearchTerm}", request.SearchTerm);
                        var data = await _combineService.GetSearchAsync(request.SearchTerm);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving search data for search term {SearchTerm}", request.SearchTerm);
                throw;
            }
        }
    }
}