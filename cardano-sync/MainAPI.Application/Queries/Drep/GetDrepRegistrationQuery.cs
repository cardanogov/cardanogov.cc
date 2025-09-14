namespace MainAPI.Application.Queries.Drep
{
    public class GetDrepRegistrationQuery : IRequest<List<DrepRegistrationTableResponseDto>?>
    {
        public string DrepId { get; }

        public GetDrepRegistrationQuery(string drepId)
        {
            DrepId = drepId;
        }
    }

    public class GetDrepRegistrationQueryHandler : IRequestHandler<GetDrepRegistrationQuery, List<DrepRegistrationTableResponseDto>?>
    {
        private readonly IDrepService _drepService;
        private readonly MainAPI.Core.Interfaces.ICacheService _cacheService;
        private readonly Microsoft.Extensions.Logging.ILogger<GetDrepRegistrationQueryHandler> _logger;

        public GetDrepRegistrationQueryHandler(
            IDrepService drepService,
            MainAPI.Core.Interfaces.ICacheService cacheService,
            Microsoft.Extensions.Logging.ILogger<GetDrepRegistrationQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<DrepRegistrationTableResponseDto>?> Handle(GetDrepRegistrationQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.DREP_REGISTRATION}_{request.DrepId}";

            try
            {
                _logger.LogInformation("Processing GetDrepRegistrationQuery for DrepId: {DrepId} with cache key: {CacheKey}", request.DrepId, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching drep registration data for DrepId: {DrepId}", request.DrepId);
                        var data = await _drepService.GetDrepRegistrationAsync(request.DrepId);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drep registration for drep {DrepId}", request.DrepId);
                throw;
            }
        }
    }
}