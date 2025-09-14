namespace MainAPI.Application.Queries.Treasury
{
    public class GetTreasuryWithdrawalsQuery : IRequest<List<TreasuryWithdrawalsResponseDto>?> { }

    public class GetTreasuryWithdrawalsQueryHandler : IRequestHandler<GetTreasuryWithdrawalsQuery, List<TreasuryWithdrawalsResponseDto>?>
    {
        private readonly ITreasuryService _treasuryService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetTreasuryWithdrawalsQueryHandler> _logger;

        public GetTreasuryWithdrawalsQueryHandler(
            ITreasuryService treasuryService,
            ICacheService cacheService,
            ILogger<GetTreasuryWithdrawalsQueryHandler> logger)
        {
            _treasuryService = treasuryService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<TreasuryWithdrawalsResponseDto>?> Handle(GetTreasuryWithdrawalsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.TREASURY_WITHDRAWALS;

            try
            {
                _logger.LogInformation("Processing GetTreasuryWithdrawalsQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching treasury withdrawals data");
                        var data = await _treasuryService.GetTreasuryWithdrawalsAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving treasury withdrawals data");
                throw;
            }
        }
    }
}