namespace MainAPI.Application.Queries.Voting
{
    public class GetVotingHistoryQuery : IRequest<VotingHistoryResponseDto?>
    {
        public int Page { get; }
        public string? Filter { get; }
        public string? Search { get; }
        public GetVotingHistoryQuery(int page, string? filter, string? search)
        {
            Page = page;
            Filter = filter;
            Search = search;
        }
    }

    public class GetVotingHistoryQueryHandler : IRequestHandler<GetVotingHistoryQuery, VotingHistoryResponseDto?>
    {
        private readonly IVotingService _votingService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetVotingHistoryQueryHandler> _logger;

        public GetVotingHistoryQueryHandler(
            IVotingService votingService,
            ICacheService cacheService,
            ILogger<GetVotingHistoryQueryHandler> logger)
        {
            _votingService = votingService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<VotingHistoryResponseDto?> Handle(GetVotingHistoryQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.VOTING_HISTORY}_{request.Page}_{request.Filter ?? ""}_{request.Search ?? ""}";

            try
            {
                _logger.LogInformation("Processing GetVotingHistoryQuery for Page: {Page} with cache key: {CacheKey}", request.Page, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching voting history data for Page: {Page}", request.Page);
                        var data = await _votingService.GetVotingHistoryAsync(request.Page, request.Filter, request.Search);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving voting history data for page {Page}", request.Page);
                throw;
            }
        }
    }
}