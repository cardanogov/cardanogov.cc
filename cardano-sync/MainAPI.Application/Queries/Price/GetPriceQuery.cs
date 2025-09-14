namespace MainAPI.Application.Queries.Price
{
    public class GetPriceQuery : IRequest<decimal?>
    {
    }

    public class GetPriceQueryHandler : IRequestHandler<GetPriceQuery, decimal?>
    {
        private readonly IPriceService _priceService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetPriceQueryHandler> _logger;

        public GetPriceQueryHandler(
            IPriceService priceService,
            ICacheService cacheService,
            ILogger<GetPriceQueryHandler> logger)
        {
            _priceService = priceService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<decimal?> Handle(GetPriceQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = "price";

            try
            {
                _logger.LogInformation("Processing GetPriceQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching price data");
                        var data = await _priceService.GetUsdPriceAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving price data");
                throw;
            }
        }
    }
}