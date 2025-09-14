namespace MainAPI.Application.Queries.Drep
{
    public class GetDrepTotalStakeApprovalThresholdQuery : IRequest<DrepPoolStakeThresholdResponseDto?>
    {
        public int EpochNo { get; }
        public string ProposalType { get; }

        public GetDrepTotalStakeApprovalThresholdQuery(int epochNo, string proposalType)
        {
            EpochNo = epochNo;
            ProposalType = proposalType;
        }
    }

    public class GetDrepTotalStakeApprovalThresholdQueryHandler : IRequestHandler<GetDrepTotalStakeApprovalThresholdQuery, DrepPoolStakeThresholdResponseDto?>
    {
        private readonly IDrepService _drepService;
        private readonly MainAPI.Core.Interfaces.ICacheService _cacheService;
        private readonly Microsoft.Extensions.Logging.ILogger<GetDrepTotalStakeApprovalThresholdQueryHandler> _logger;

        public GetDrepTotalStakeApprovalThresholdQueryHandler(
            IDrepService drepService,
            MainAPI.Core.Interfaces.ICacheService cacheService,
            Microsoft.Extensions.Logging.ILogger<GetDrepTotalStakeApprovalThresholdQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<DrepPoolStakeThresholdResponseDto?> Handle(GetDrepTotalStakeApprovalThresholdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.DREP_TOTAL_STAKE_APPROVAL_THRESHOLD}_{request.ProposalType}_{request.EpochNo}";

            try
            {
                _logger.LogInformation("Processing GetDrepTotalStakeApprovalThresholdQuery for EpochNo: {EpochNo} with cache key: {CacheKey}", request.EpochNo, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching drep total stake approval threshold data for EpochNo: {EpochNo}", request.EpochNo);
                        var data = await _drepService.GetDrepTotalStakeApprovalThresholdAsync(request.EpochNo, request.ProposalType);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drep total stake approval threshold for EpochNo {EpochNo}", request.EpochNo);
                throw;
            }
        }
    }
}