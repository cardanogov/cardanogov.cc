namespace MainAPI.Application.Queries.Combine
{
    public class GetGovernanceParametersQuery : IRequest<List<GovernanceParametersResponseDto>>
    {
    }

    public class GetGovernanceParametersQueryHandler : IRequestHandler<GetGovernanceParametersQuery, List<GovernanceParametersResponseDto>>
    {
        private readonly ICombineService _combineService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetGovernanceParametersQueryHandler> _logger;

        public GetGovernanceParametersQueryHandler(
            ICombineService combineService,
            ICacheService cacheService,
            ILogger<GetGovernanceParametersQueryHandler> logger)
        {
            _combineService = combineService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<GovernanceParametersResponseDto>> Handle(GetGovernanceParametersQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = "governance_parameters";

            try
            {
                _logger.LogInformation("Processing GetGovernanceParametersQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching governance parameters data");
                        var data = await _combineService.GetGovernanceParametersAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving governance parameters data");
                throw;
            }
        }
    }
}