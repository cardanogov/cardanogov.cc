namespace MainAPI.Application.Queries.Voting
{
    public class GetVoteListQuery : IRequest<List<VoteListResponseDto>?> { }

    public class GetVoteListQueryHandler : IRequestHandler<GetVoteListQuery, List<VoteListResponseDto>?>
    {
        private readonly IVotingService _votingService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetVoteListQueryHandler> _logger;

        public GetVoteListQueryHandler(
            IVotingService votingService,
            ICacheService cacheService,
            ILogger<GetVoteListQueryHandler> logger)
        {
            _votingService = votingService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<VoteListResponseDto>?> Handle(GetVoteListQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.VOTE_LIST;

            try
            {
                _logger.LogInformation("Processing GetVoteListQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching vote list data");
                        var data = await _votingService.GetVoteListAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vote list data");
                throw;
            }
        }
    }
}