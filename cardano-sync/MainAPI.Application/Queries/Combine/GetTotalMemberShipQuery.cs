namespace MainAPI.Application.Queries.Combine
{
    public class GetTotalMemberShipQuery : IRequest<MembershipDataResponseDto>
    {
    }

    public class GetTotalMemberShipQueryHandler : IRequestHandler<GetTotalMemberShipQuery, MembershipDataResponseDto>
    {
        private readonly ICombineService _combineService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetTotalMemberShipQueryHandler> _logger;

        public GetTotalMemberShipQueryHandler(
            ICombineService combineService,
            ICacheService cacheService,
            ILogger<GetTotalMemberShipQueryHandler> logger)
        {
            _combineService = combineService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<MembershipDataResponseDto> Handle(GetTotalMemberShipQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = "total_membership";

            try
            {
                _logger.LogInformation("Processing GetTotalMemberShipQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching total membership data");
                        var data = await _combineService.GetTotalsMembershipAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total membership data");
                throw;
            }
        }
    }
}
