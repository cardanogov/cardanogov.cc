namespace MainAPI.Application.Queries.Combine
{
    public class GetParticipateInVotingQuery : IRequest<ParticipateInVotingResponseDto>
    {
    }

    public class GetParticipateInVotingQueryHandler : IRequestHandler<GetParticipateInVotingQuery, ParticipateInVotingResponseDto>
    {
        private readonly ICombineService _combineService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetParticipateInVotingQueryHandler> _logger;

        public GetParticipateInVotingQueryHandler(
            ICombineService combineService,
            ICacheService cacheService,
            ILogger<GetParticipateInVotingQueryHandler> logger)
        {
            _combineService = combineService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<ParticipateInVotingResponseDto> Handle(GetParticipateInVotingQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = "participate_in_voting";

            try
            {
                _logger.LogInformation("Processing GetParticipateInVotingQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching participate in voting data");
                        var data = await _combineService.GetParticipateInVotingAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving participate in voting data");
                throw;
            }
        }
    }
}