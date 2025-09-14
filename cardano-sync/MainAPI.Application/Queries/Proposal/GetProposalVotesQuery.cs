namespace MainAPI.Application.Queries.Proposal
{
    public class GetProposalVotesQuery : IRequest<ProposalVotesResponseDto?>
    {
        public string ProposalId { get; set; }
        public int Page { get; set; }
        public string? Filter { get; set; }
        public string? Search { get; set; }

        public GetProposalVotesQuery(string proposalId, int page, string? filter, string? search)
        {
            ProposalId = proposalId;
            Page = page;
            Filter = filter;
            Search = search;
        }
    }

    public class GetProposalVotesQueryHandler : IRequestHandler<GetProposalVotesQuery, ProposalVotesResponseDto?>
    {
        private readonly IProposalService _proposalService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetProposalVotesQueryHandler> _logger;

        public GetProposalVotesQueryHandler(
            IProposalService proposalService,
            ICacheService cacheService,
            ILogger<GetProposalVotesQueryHandler> logger)
        {
            _proposalService = proposalService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<ProposalVotesResponseDto?> Handle(GetProposalVotesQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.PROPOSAL_VOTES}_{request.ProposalId}_{request.Page}_{request.Filter ?? ""}_{request.Search ?? ""}";

            try
            {
                _logger.LogInformation("Processing GetProposalVotesQuery for ProposalId: {ProposalId}, Page: {Page} with cache key: {CacheKey}", request.ProposalId, request.Page, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching proposal votes data for ProposalId: {ProposalId}, Page: {Page}", request.ProposalId, request.Page);
                        var data = await _proposalService.GetProposalVotesAsync(request.ProposalId, request.Page, request.Filter, request.Search);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving proposal votes for proposal {ProposalId}, page {Page}", request.ProposalId, request.Page);
                throw;
            }
        }
    }
}
