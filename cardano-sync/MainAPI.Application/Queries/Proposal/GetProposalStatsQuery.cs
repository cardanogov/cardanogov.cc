namespace MainAPI.Application.Queries.Proposal
{
    public class GetProposalStatsQuery : IRequest<ProposalStatsResponseDto?>
    {
        // No parameters needed for this query
    }

    public class GetProposalStatsQueryHandler : IRequestHandler<GetProposalStatsQuery, ProposalStatsResponseDto?>
    {
        private readonly IProposalService _proposalService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetProposalStatsQueryHandler> _logger;

        public GetProposalStatsQueryHandler(
            IProposalService proposalService,
            ICacheService cacheService,
            ILogger<GetProposalStatsQueryHandler> logger)
        {
            _proposalService = proposalService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<ProposalStatsResponseDto?> Handle(GetProposalStatsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.PROPOSAL_STATS;

            try
            {
                _logger.LogInformation("Processing GetProposalStatsQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching proposal stats data");
                        var data = await _proposalService.GetProposalStatsAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving proposal stats data");
                throw;
            }
        }
    }
}
