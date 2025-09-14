namespace MainAPI.Application.Queries.Committee
{
    public class GetCommitteeVotesQuery : IRequest<List<CommitteeVotesResponseDto>?>
    {
        public string CcHotId { get; }

        public GetCommitteeVotesQuery(string ccHotId)
        {
            CcHotId = ccHotId;
        }
    }

    public class GetCommitteeVotesQueryHandler : IRequestHandler<GetCommitteeVotesQuery, List<CommitteeVotesResponseDto>?>
    {
        private readonly ICommitteeService _committeeService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetCommitteeVotesQueryHandler> _logger;

        public GetCommitteeVotesQueryHandler(
            ICommitteeService committeeService,
            ICacheService cacheService,
            ILogger<GetCommitteeVotesQueryHandler> logger)
        {
            _committeeService = committeeService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<CommitteeVotesResponseDto>?> Handle(GetCommitteeVotesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = $"committee_votes_{request.CcHotId}";

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching committee votes data for cc_hot_id: {CcHotId}", request.CcHotId);
                        var data = await _committeeService.GetCommitteeVotesAsync(request.CcHotId);
                        return data ?? new List<CommitteeVotesResponseDto>();
                    },
                    300 // 5 minutes cache as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GetCommitteeVotesQuery for cc_hot_id: {CcHotId}", request.CcHotId);
                throw;
            }
        }
    }
}