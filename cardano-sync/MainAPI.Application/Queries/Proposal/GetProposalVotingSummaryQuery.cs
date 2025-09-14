namespace MainAPI.Application.Queries.Proposal
{
    public class GetProposalVotingSummaryQuery : IRequest<ProposalVotingSummaryResponseDto?>
    {
        public string GovId { get; set; }

        public GetProposalVotingSummaryQuery(string govId)
        {
            GovId = govId;
        }
    }

    public class GetProposalVotingSummaryQueryHandler : IRequestHandler<GetProposalVotingSummaryQuery, ProposalVotingSummaryResponseDto?>
    {
        private readonly IProposalService _proposalService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetProposalVotingSummaryQueryHandler> _logger;

        public GetProposalVotingSummaryQueryHandler(
            IProposalService proposalService,
            ICacheService cacheService,
            ILogger<GetProposalVotingSummaryQueryHandler> logger)
        {
            _proposalService = proposalService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<ProposalVotingSummaryResponseDto?> Handle(GetProposalVotingSummaryQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.PROPOSAL_VOTING_SUMMARY}_{request.GovId}";

            try
            {
                _logger.LogInformation("Processing GetProposalVotingSummaryQuery for GovId: {GovId} with cache key: {CacheKey}", request.GovId, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching proposal voting summary data for GovId: {GovId}", request.GovId);
                        var data = await _proposalService.GetProposalVotingSummaryAsync(request.GovId);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving proposal voting summary for gov_id {GovId}", request.GovId);
                throw;
            }
        }
    }
}
