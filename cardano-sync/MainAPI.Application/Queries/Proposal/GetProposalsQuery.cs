namespace MainAPI.Application.Queries.Proposal
{
    public class GetProposalsQuery : IRequest<GovernanceActionResponseDto?>
    {
        public bool? IsLive { get; set; }

        public GetProposalsQuery(bool? isLive)
        {
            IsLive = isLive;
        }
    }

    public class GetProposalsQueryHandler : IRequestHandler<GetProposalsQuery, GovernanceActionResponseDto?>
    {
        private readonly IProposalService _proposalService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetProposalsQueryHandler> _logger;

        public GetProposalsQueryHandler(
            IProposalService proposalService,
            ICacheService cacheService,
            ILogger<GetProposalsQueryHandler> logger)
        {
            _proposalService = proposalService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<GovernanceActionResponseDto?> Handle(GetProposalsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = GetCacheKey(request.IsLive);

            try
            {
                _logger.LogInformation("Processing GetProposalsQuery with IsLive: {IsLive} and cache key: {CacheKey}", request.IsLive, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching proposals data for IsLive: {IsLive}", request.IsLive);
                        var data = await _proposalService.GetProposalsAsync(request.IsLive);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving proposals data for IsLive: {IsLive}", request.IsLive);
                throw;
            }
        }

        private static string GetCacheKey(bool? isLive)
        {
            return isLive switch
            {
                true => CacheKeys.PROPOSAL_LIVE,
                false => CacheKeys.PROPOSAL_EXPIRED,
                null => CacheKeys.PROPOSAL_COMBINED
            };
        }
    }
}
