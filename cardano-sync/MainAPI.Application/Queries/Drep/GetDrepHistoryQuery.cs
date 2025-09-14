namespace MainAPI.Application.Queries.Drep
{
    public class GetDrepHistoryQuery : IRequest<DrepHistoryResponseDto?>
    {
        public int EpochNo { get; }
        public string DrepId { get; }

        public GetDrepHistoryQuery(int epochNo, string drepId)
        {
            EpochNo = epochNo;
            DrepId = drepId;
        }
    }

    public class GetDrepHistoryQueryHandler : IRequestHandler<GetDrepHistoryQuery, DrepHistoryResponseDto?>
    {
        private readonly IDrepService _drepService;
        private readonly MainAPI.Core.Interfaces.ICacheService _cacheService;
        private readonly Microsoft.Extensions.Logging.ILogger<GetDrepHistoryQueryHandler> _logger;

        public GetDrepHistoryQueryHandler(
            IDrepService drepService,
            MainAPI.Core.Interfaces.ICacheService cacheService,
            Microsoft.Extensions.Logging.ILogger<GetDrepHistoryQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<DrepHistoryResponseDto?> Handle(GetDrepHistoryQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.DREP_HISTORY}_{request.DrepId}";

            try
            {
                _logger.LogInformation("Processing GetDrepHistoryQuery for DrepId: {DrepId} with cache key: {CacheKey}", request.DrepId, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching drep history data for DrepId: {DrepId}", request.DrepId);
                        var data = await _drepService.GetDrepHistoryAsync(request.EpochNo, request.DrepId);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drep history for drep {DrepId}", request.DrepId);
                throw;
            }
        }
    }
}