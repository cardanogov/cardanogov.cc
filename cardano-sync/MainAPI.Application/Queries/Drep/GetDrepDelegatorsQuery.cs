namespace MainAPI.Application.Queries.Drep
{
    public class GetDrepDelegatorsQuery : IRequest<List<DrepDelegatorsResponseDto>?>
    {
        public string DrepId { get; }

        public GetDrepDelegatorsQuery(string drepId)
        {
            DrepId = drepId;
        }
    }

    public class GetDrepDelegatorsQueryHandler : IRequestHandler<GetDrepDelegatorsQuery, List<DrepDelegatorsResponseDto>?>
    {
        private readonly IDrepService _drepService;
        private readonly MainAPI.Core.Interfaces.ICacheService _cacheService;
        private readonly Microsoft.Extensions.Logging.ILogger<GetDrepDelegatorsQueryHandler> _logger;

        public GetDrepDelegatorsQueryHandler(
            IDrepService drepService,
            MainAPI.Core.Interfaces.ICacheService cacheService,
            Microsoft.Extensions.Logging.ILogger<GetDrepDelegatorsQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<DrepDelegatorsResponseDto>?> Handle(GetDrepDelegatorsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.DREP_DELEGATORS}_{request.DrepId}";

            try
            {
                _logger.LogInformation("Processing GetDrepDelegatorsQuery for DrepId: {DrepId} with cache key: {CacheKey}", request.DrepId, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching drep delegators data for DrepId: {DrepId}", request.DrepId);
                        var data = await _drepService.GetDrepDelegatorsAsync(request.DrepId);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drep delegators for drep {DrepId}", request.DrepId);
                throw;
            }
        }
    }
}