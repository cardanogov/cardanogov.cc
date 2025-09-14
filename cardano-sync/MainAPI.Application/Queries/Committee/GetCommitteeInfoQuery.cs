namespace MainAPI.Application.Queries.Committee
{
    public class GetCommitteeInfoQuery : IRequest<List<CommitteeInfoResponseDto>?>
    {
    }

    public class GetCommitteeInfoQueryHandler : IRequestHandler<GetCommitteeInfoQuery, List<CommitteeInfoResponseDto>?>
    {
        private readonly ICommitteeService _committeeService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetCommitteeInfoQueryHandler> _logger;

        public GetCommitteeInfoQueryHandler(
            ICommitteeService committeeService,
            ICacheService cacheService,
            ILogger<GetCommitteeInfoQueryHandler> logger)
        {
            _committeeService = committeeService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<CommitteeInfoResponseDto>?> Handle(GetCommitteeInfoQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = "committee_info";

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching committee info data from service");
                        var data = await _committeeService.GetCommitteeInfoAsync();
                        return data ?? new List<CommitteeInfoResponseDto>();
                    },
                    360 // 6 minutes cache as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GetCommitteeInfoQuery");
                throw;
            }
        }
    }
}