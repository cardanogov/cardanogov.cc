namespace MainAPI.Application.Queries.Drep
{
    public class GetDrepCardDataByIdQuery : IRequest<DrepCardDataByIdResponseDto?>
    {
        public string DrepId { get; }

        public GetDrepCardDataByIdQuery(string drepId)
        {
            DrepId = drepId;
        }
    }

    public class GetDrepCardDataByIdQueryHandler : IRequestHandler<GetDrepCardDataByIdQuery, DrepCardDataByIdResponseDto?>
    {
        private readonly IDrepService _drepService;
        private readonly MainAPI.Core.Interfaces.ICacheService _cacheService;
        private readonly Microsoft.Extensions.Logging.ILogger<GetDrepCardDataByIdQueryHandler> _logger;

        public GetDrepCardDataByIdQueryHandler(
            IDrepService drepService,
            MainAPI.Core.Interfaces.ICacheService cacheService,
            Microsoft.Extensions.Logging.ILogger<GetDrepCardDataByIdQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<DrepCardDataByIdResponseDto?> Handle(GetDrepCardDataByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.DREP_CARD_DATA_BY_ID}_{request.DrepId}";

            try
            {
                _logger.LogInformation("Processing GetDrepCardDataByIdQuery for DrepId: {DrepId} with cache key: {CacheKey}", request.DrepId, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching drep card data by id for DrepId: {DrepId}", request.DrepId);
                        var data = await _drepService.GetDrepCardDataByIdAsync(request.DrepId);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drep card data by id for drep {DrepId}", request.DrepId);
                throw;
            }
        }
    }
}