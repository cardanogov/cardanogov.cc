namespace MainAPI.Application.Queries.Voting
{
    public class GetVotingCardDataQuery : IRequest<VotingCardInfoDto?> { }

    public class GetVotingCardDataQueryHandler : IRequestHandler<GetVotingCardDataQuery, VotingCardInfoDto?>
    {
        private readonly IVotingService _votingService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetVotingCardDataQueryHandler> _logger;

        public GetVotingCardDataQueryHandler(
            IVotingService votingService,
            ICacheService cacheService,
            ILogger<GetVotingCardDataQueryHandler> logger)
        {
            _votingService = votingService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<VotingCardInfoDto?> Handle(GetVotingCardDataQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = "voting_card_data";

            try
            {
                _logger.LogInformation("Processing GetVotingCardDataQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching voting card data");
                        var data = await _votingService.GetVotingCardDataAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving voting card data");
                throw;
            }
        }
    }
}