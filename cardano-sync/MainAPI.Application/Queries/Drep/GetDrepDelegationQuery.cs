namespace MainAPI.Application.Queries.Drep
{
    public class GetDrepDelegationQuery : IRequest<DrepDelegationResponseDto?>
    {
        public string DrepId { get; }

        public GetDrepDelegationQuery(string drepId)
        {
            DrepId = drepId;
        }
    }

    public class GetDrepDelegationQueryHandler : IRequestHandler<GetDrepDelegationQuery, DrepDelegationResponseDto?>
    {
        private readonly IDrepService _drepService;
        private readonly MainAPI.Core.Interfaces.ICacheService _cacheService;
        private readonly Microsoft.Extensions.Logging.ILogger<GetDrepDelegationQueryHandler> _logger;

        public GetDrepDelegationQueryHandler(
            IDrepService drepService,
            MainAPI.Core.Interfaces.ICacheService cacheService,
            Microsoft.Extensions.Logging.ILogger<GetDrepDelegationQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<DrepDelegationResponseDto?> Handle(GetDrepDelegationQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.DREP_DELEGATION}_{request.DrepId}";

            try
            {
                _logger.LogInformation("Processing GetDrepDelegationQuery for DrepId: {DrepId} with cache key: {CacheKey}", request.DrepId, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching drep delegation data for DrepId: {DrepId}", request.DrepId);
                        var data = await _drepService.GetDrepDelegationAsync(request.DrepId);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drep delegation for drep {DrepId}", request.DrepId);
                throw;
            }
        }
    }
}