namespace MainAPI.Application.Queries.Drep
{
    public class GetDrepNewRegisterQuery : IRequest<List<DrepNewRegisterResponseDto>?>
    {
    }

    public class GetDrepNewRegisterQueryHandler : IRequestHandler<GetDrepNewRegisterQuery, List<DrepNewRegisterResponseDto>?>
    {
        private readonly IDrepService _drepService;
        private readonly MainAPI.Core.Interfaces.ICacheService _cacheService;
        private readonly Microsoft.Extensions.Logging.ILogger<GetDrepNewRegisterQueryHandler> _logger;

        public GetDrepNewRegisterQueryHandler(
            IDrepService drepService,
            MainAPI.Core.Interfaces.ICacheService cacheService,
            Microsoft.Extensions.Logging.ILogger<GetDrepNewRegisterQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<DrepNewRegisterResponseDto>?> Handle(GetDrepNewRegisterQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.DREP_NEW_REGISTER;

            try
            {
                _logger.LogInformation("Processing GetDrepNewRegisterQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching drep new register data");
                        var data = await _drepService.GetDrepNewRegisterAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drep new register data");
                throw;
            }
        }
    }
}