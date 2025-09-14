namespace MainAPI.Application.Queries.Drep
{
    public class GetDrepEpochSummaryQuery : IRequest<double>
    {
        public int EpochNo { get; }

        public GetDrepEpochSummaryQuery(int epochNo)
        {
            EpochNo = epochNo;
        }
    }

    public class GetDrepEpochSummaryQueryHandler : IRequestHandler<GetDrepEpochSummaryQuery, double>
    {
        private readonly IDrepService _drepService;
        private readonly MainAPI.Core.Interfaces.ICacheService _cacheService;
        private readonly Microsoft.Extensions.Logging.ILogger<GetDrepEpochSummaryQueryHandler> _logger;

        public GetDrepEpochSummaryQueryHandler(
            IDrepService drepService,
            MainAPI.Core.Interfaces.ICacheService cacheService,
            Microsoft.Extensions.Logging.ILogger<GetDrepEpochSummaryQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<double> Handle(GetDrepEpochSummaryQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.DREP_EPOCH_SUMMARY}_{request.EpochNo}";

            try
            {
                _logger.LogInformation("Processing GetDrepEpochSummaryQuery for epochNo: {EpochNo} with cache key: {CacheKey}", request.EpochNo, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching drep epoch summary data for epochNo: {EpochNo}", request.EpochNo);
                        var data = await _drepService.GetDrepEpochSummaryAsync(request.EpochNo);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drep epoch summary for epochNo {EpochNo}", request.EpochNo);
                throw;
            }
        }
    }
}