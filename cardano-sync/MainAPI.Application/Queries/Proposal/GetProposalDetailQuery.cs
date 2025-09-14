namespace MainAPI.Application.Queries.Proposal
{
    public class GetProposalDetailQuery : IRequest<GovernanceActionResponseDto?>
    {
        public string? ProposalId { get; set; }
        public bool? IsLive { get; set; }

        public GetProposalDetailQuery(string? proposalId, bool? isLive)
        {
            ProposalId = proposalId;
            IsLive = isLive;
        }
    }

    public class GetProposalDetailQueryHandler : IRequestHandler<GetProposalDetailQuery, GovernanceActionResponseDto?>
    {
        private readonly IProposalService _proposalService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetProposalDetailQueryHandler> _logger;

        public GetProposalDetailQueryHandler(
            IProposalService proposalService,
            ICacheService cacheService,
            ILogger<GetProposalDetailQueryHandler> logger)
        {
            _proposalService = proposalService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<GovernanceActionResponseDto?> Handle(GetProposalDetailQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = GetCacheKey(request.ProposalId, request.IsLive);

            try
            {
                _logger.LogInformation("Processing GetProposalDetailQuery for ProposalId: {ProposalId}, IsLive: {IsLive} with cache key: {CacheKey}",
                    request.ProposalId, request.IsLive, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching proposal detail data for ProposalId: {ProposalId}, IsLive: {IsLive}",
                            request.ProposalId, request.IsLive);
                        var data = await _proposalService.GetProposalDetailAsync(request.ProposalId, request.IsLive);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving proposal detail for proposal {ProposalId}, IsLive: {IsLive}",
                    request.ProposalId, request.IsLive);
                throw;
            }
        }

        private static string GetCacheKey(string? proposalId, bool? isLive)
        {
            var baseKey = isLive switch
            {
                true => CacheKeys.PROPOSAL_LIVE_DETAIL,
                false => CacheKeys.PROPOSAL_EXPIRED_DETAIL,
                null => CacheKeys.PROPOSAL_DETAIL_COMBINED
            };

            return $"{baseKey}_{proposalId}";
        }
    }
}
