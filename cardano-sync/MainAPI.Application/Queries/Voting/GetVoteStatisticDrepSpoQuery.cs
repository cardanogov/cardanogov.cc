namespace MainAPI.Application.Queries.Voting
{
    public class GetVoteStatisticDrepSpoQuery : IRequest<List<VoteStatisticResponseDto>?> { }

    public class GetVoteStatisticDrepSpoQueryHandler : IRequestHandler<GetVoteStatisticDrepSpoQuery, List<VoteStatisticResponseDto>?>
    {
        private readonly IVotingService _votingService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetVoteStatisticDrepSpoQueryHandler> _logger;

        public GetVoteStatisticDrepSpoQueryHandler(
            IVotingService votingService,
            ICacheService cacheService,
            ILogger<GetVoteStatisticDrepSpoQueryHandler> logger)
        {
            _votingService = votingService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<VoteStatisticResponseDto>?> Handle(GetVoteStatisticDrepSpoQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.VOTE_STATISTIC_DREP_SPO;

            try
            {
                _logger.LogInformation("Processing GetVoteStatisticDrepSpoQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching vote statistic drep spo data");
                        var data = await _votingService.GetVoteStatisticDrepSpoAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vote statistic drep spo data");
                throw;
            }
        }
    }
}