namespace MainAPI.Application.Queries.Combine
{
    public class GetAllocationQuery : IRequest<AllocationResponseDto>
    {
    }

    public class GetAllocationQueryHandler : IRequestHandler<GetAllocationQuery, AllocationResponseDto>
    {
        private readonly ICombineService _combineService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetAllocationQueryHandler> _logger;

        public GetAllocationQueryHandler(
            ICombineService combineService,
            ICacheService cacheService,
            ILogger<GetAllocationQueryHandler> logger)
        {
            _combineService = combineService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<AllocationResponseDto> Handle(GetAllocationQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = "allocation";

            try
            {
                _logger.LogInformation("Processing GetAllocationQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching allocation data");
                        var data = await _combineService.GetAllocationAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving allocation data");
                throw;
            }
        }
    }
}