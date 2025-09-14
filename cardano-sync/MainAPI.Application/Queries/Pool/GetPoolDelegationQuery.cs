namespace MainAPI.Application.Queries.Pool
{
    public class GetPoolDelegationQuery : IRequest<DelegationResponseDto?>
    {
        public string PoolId { get; }
        public int Page { get; }
        public int PageSize { get; }
        public string? SortBy { get; }
        public string? SortOrder { get; }

        public GetPoolDelegationQuery(string poolId, int page = 1, int pageSize = 20, string? sortBy = null, string? sortOrder = null)
        {
            PoolId = poolId;
            Page = page;
            PageSize = pageSize;
            SortBy = sortBy;
            SortOrder = sortOrder;
        }
    }

    public class GetPoolDelegationQueryHandler : IRequestHandler<GetPoolDelegationQuery, DelegationResponseDto?>
    {
        private readonly SharedLibrary.Interfaces.IPoolService _poolService;
        private readonly Microsoft.Extensions.Logging.ILogger<GetPoolDelegationQueryHandler> _logger;

        public GetPoolDelegationQueryHandler(
            SharedLibrary.Interfaces.IPoolService poolService,
            Microsoft.Extensions.Logging.ILogger<GetPoolDelegationQueryHandler> logger)
        {
            _poolService = poolService;
            _logger = logger;
        }

        public async Task<DelegationResponseDto?> Handle(GetPoolDelegationQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing GetPoolDelegationQuery for PoolId: {PoolId}, Page: {Page}, PageSize: {PageSize}",
                    request.PoolId, request.Page, request.PageSize);

                var result = await _poolService.GetPoolDelegationAsync(
                    request.PoolId,
                    request.Page,
                    request.PageSize,
                    request.SortBy,
                    request.SortOrder);

                _logger.LogInformation("Successfully retrieved delegation data for PoolId: {PoolId}", request.PoolId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pool delegation for pool_id: {PoolId}", request.PoolId);
                throw;
            }
        }
    }
}