namespace MainAPI.Application.Queries.Drep
{
    public class GetDrepMetadataQuery : IRequest<DrepMetadataResponseDto?>
    {
        public string DrepId { get; }

        public GetDrepMetadataQuery(string drepId)
        {
            DrepId = drepId;
        }
    }

    public class GetDrepMetadataQueryHandler : IRequestHandler<GetDrepMetadataQuery, DrepMetadataResponseDto?>
    {
        private readonly IDrepService _drepService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetDrepMetadataQueryHandler> _logger;

        public GetDrepMetadataQueryHandler(
            IDrepService drepService,
            ICacheService cacheService,
            ILogger<GetDrepMetadataQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<DrepMetadataResponseDto?> Handle(GetDrepMetadataQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.DREP_METADATA}_{request.DrepId}";

            try
            {
                _logger.LogInformation("Processing GetDrepMetadataQuery for DrepId: {DrepId} with cache key: {CacheKey}", request.DrepId, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching drep metadata data for DrepId: {DrepId}", request.DrepId);
                        var data = await _drepService.GetDrepMetadataAsync(request.DrepId);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drep metadata for drep {DrepId}", request.DrepId);
                throw;
            }
        }
    }
}