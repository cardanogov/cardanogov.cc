namespace MainAPI.Application.Queries.Drep
{
    public class GetDrepUpdatesQuery : IRequest<List<DrepsUpdatesResponseDto>?>
    {
        public string DrepId { get; }

        public GetDrepUpdatesQuery(string drepId)
        {
            DrepId = drepId;
        }
    }

    public class GetDrepUpdatesQueryHandler : IRequestHandler<GetDrepUpdatesQuery, List<DrepsUpdatesResponseDto>?>
    {
        private readonly IDrepService _drepService;
        private readonly MainAPI.Core.Interfaces.ICacheService _cacheService;
        private readonly Microsoft.Extensions.Logging.ILogger<GetDrepUpdatesQueryHandler> _logger;

        public GetDrepUpdatesQueryHandler(
            IDrepService drepService,
            MainAPI.Core.Interfaces.ICacheService cacheService,
            Microsoft.Extensions.Logging.ILogger<GetDrepUpdatesQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<DrepsUpdatesResponseDto>?> Handle(GetDrepUpdatesQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.DREP_UPDATES}_{request.DrepId}";

            try
            {
                _logger.LogInformation("Processing GetDrepUpdatesQuery for DrepId: {DrepId} with cache key: {CacheKey}", request.DrepId, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching drep updates data for DrepId: {DrepId}", request.DrepId);
                        var data = await _drepService.GetDrepUpdatesAsync(request.DrepId);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drep updates for drep {DrepId}", request.DrepId);
                throw;
            }
        }
    }
}