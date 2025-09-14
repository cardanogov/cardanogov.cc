namespace MainAPI.Application.Queries.Drep
{
    public class GetDrepVotesQuery : IRequest<DrepVoteInfoResponseDto?>
    {
        public string DrepId { get; }

        public GetDrepVotesQuery(string drepId)
        {
            DrepId = drepId;
        }
    }

    public class GetDrepVotesQueryHandler : IRequestHandler<GetDrepVotesQuery, DrepVoteInfoResponseDto?>
    {
        private readonly IDrepService _drepService;
        private readonly MainAPI.Core.Interfaces.ICacheService _cacheService;
        private readonly Microsoft.Extensions.Logging.ILogger<GetDrepVotesQueryHandler> _logger;

        public GetDrepVotesQueryHandler(
            IDrepService drepService,
            MainAPI.Core.Interfaces.ICacheService cacheService,
            Microsoft.Extensions.Logging.ILogger<GetDrepVotesQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<DrepVoteInfoResponseDto?> Handle(GetDrepVotesQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.DREP_VOTES}_{request.DrepId}";

            try
            {
                _logger.LogInformation("Processing GetDrepVotesQuery for DrepId: {DrepId} with cache key: {CacheKey}", request.DrepId, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching drep votes data for DrepId: {DrepId}", request.DrepId);
                        var data = await _drepService.GetDrepVotesAsync(request.DrepId);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drep votes for drep {DrepId}", request.DrepId);
                throw;
            }
        }
    }
}