namespace MainAPI.Application.Queries.Voting
{
    public class GetVoteParticipationIndexQuery : IRequest<int?> { }

    public class GetVoteParticipationIndexQueryHandler : IRequestHandler<GetVoteParticipationIndexQuery, int?>
    {
        private readonly IVotingService _votingService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetVoteParticipationIndexQueryHandler> _logger;

        public GetVoteParticipationIndexQueryHandler(
            IVotingService votingService,
            ICacheService cacheService,
            ILogger<GetVoteParticipationIndexQueryHandler> logger)
        {
            _votingService = votingService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<int?> Handle(GetVoteParticipationIndexQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.VOTE_PARTICIPATION_INDEX;

            try
            {
                _logger.LogInformation("Processing GetVoteParticipationIndexQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching vote participation index data");
                        var data = await _votingService.GetVoteParticipationIndexAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vote participation index data");
                throw;
            }
        }
    }
}