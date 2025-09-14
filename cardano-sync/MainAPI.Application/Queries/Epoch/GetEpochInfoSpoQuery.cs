namespace MainAPI.Application.Queries.Epoch
{
    public class GetEpochInfoSpoQuery : IRequest<int?>
    {
        public int EpochNo { get; set; }

        public GetEpochInfoSpoQuery(int epochNo)
        {
            EpochNo = epochNo;
        }
    }

    public class GetEpochInfoSpoQueryHandler : IRequestHandler<GetEpochInfoSpoQuery, int?>
    {
        private readonly IEpochService _epochService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetEpochInfoSpoQueryHandler> _logger;

        public GetEpochInfoSpoQueryHandler(
            IEpochService epochService,
            ICacheService cacheService,
            ILogger<GetEpochInfoSpoQueryHandler> logger)
        {
            _epochService = epochService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<int?> Handle(GetEpochInfoSpoQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.EPOCH_INFO_SPO}_{request.EpochNo}";

            try
            {
                _logger.LogInformation("Processing GetEpochInfoSpoQuery for EpochNo: {EpochNo} with cache key: {CacheKey}", request.EpochNo, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching epoch info spo data for EpochNo: {EpochNo}", request.EpochNo);
                        var data = await _epochService.GetEpochInfoSpoAsync(request.EpochNo);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving epoch info spo for epoch {EpochNo}", request.EpochNo);
                throw;
            }
        }
    }
}