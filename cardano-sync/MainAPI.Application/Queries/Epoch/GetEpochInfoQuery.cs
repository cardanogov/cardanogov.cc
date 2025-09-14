namespace MainAPI.Application.Queries.Epoch
{
    public class GetEpochInfoQuery : IRequest<EpochInfoResponseDto?>
    {
        public int EpochNo { get; set; }

        public GetEpochInfoQuery(int epochNo)
        {
            EpochNo = epochNo;
        }
    }

    public class GetEpochInfoQueryHandler : IRequestHandler<GetEpochInfoQuery, EpochInfoResponseDto?>
    {
        private readonly IEpochService _epochService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetEpochInfoQueryHandler> _logger;

        public GetEpochInfoQueryHandler(
            IEpochService epochService,
            ICacheService cacheService,
            ILogger<GetEpochInfoQueryHandler> logger)
        {
            _epochService = epochService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<EpochInfoResponseDto?> Handle(GetEpochInfoQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.EPOCH_INFO}_{request.EpochNo}";

            try
            {
                _logger.LogInformation("Processing GetEpochInfoQuery for EpochNo: {EpochNo} with cache key: {CacheKey}", request.EpochNo, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching epoch info data for EpochNo: {EpochNo}", request.EpochNo);
                        var data = await _epochService.GetEpochInfoAsync(request.EpochNo);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving epoch info for epoch {EpochNo}", request.EpochNo);
                throw;
            }
        }
    }
}