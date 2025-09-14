namespace MainAPI.Application.Queries.Drep
{
    public class GetDrepVoteInfoQuery : IRequest<List<DrepVoteInfoResponseDto>?>
    {
        public string DrepId { get; }

        public GetDrepVoteInfoQuery(string drepId)
        {
            DrepId = drepId;
        }
    }

    public class GetDrepVoteInfoQueryHandler : IRequestHandler<GetDrepVoteInfoQuery, List<DrepVoteInfoResponseDto>?>
    {
        private readonly IDrepService _drepService;
        private readonly MainAPI.Core.Interfaces.ICacheService _cacheService;
        private readonly Microsoft.Extensions.Logging.ILogger<GetDrepVoteInfoQueryHandler> _logger;

        public GetDrepVoteInfoQueryHandler(
            IDrepService drepService,
            MainAPI.Core.Interfaces.ICacheService cacheService,
            Microsoft.Extensions.Logging.ILogger<GetDrepVoteInfoQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<DrepVoteInfoResponseDto>?> Handle(GetDrepVoteInfoQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.DREP_VOTE_INFO}_{request.DrepId}";

            try
            {
                _logger.LogInformation("Processing GetDrepVoteInfoQuery for DrepId: {DrepId} with cache key: {CacheKey}", request.DrepId, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching drep vote info data for DrepId: {DrepId}", request.DrepId);
                        var data = await _drepService.GetDrepVoteInfoAsync(request.DrepId);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drep vote info for drep {DrepId}", request.DrepId);
                throw;
            }
        }
    }
}