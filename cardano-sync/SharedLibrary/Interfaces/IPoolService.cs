using SharedLibrary.DTOs;

namespace SharedLibrary.Interfaces
{
    public interface IPoolService
    {
        Task<int?> GetTotalPoolAsync();
        Task<List<TotalInfoResponseDto>?> GetTotalsAsync(int epoch_no);
        Task<object?> GetPoolMetadataAsync(string pool_id);
        Task<object?> GetPoolStakeSnapshotAsync(string _pool_bech32);
        Task<List<SpoVotingPowerHistoryResponseDto>?> GetSpoVotingPowerHistoryAsync();
        Task<AdaStatisticsResponseDto?> GetAdaStatisticsAsync();
        Task<AdaStatisticsPercentageResponseDto?> GetAdaStatisticsPercentageAsync();
        Task<PoolResponseDto?> GetPoolListAsync(int page, int pageSize, string? status, string? search);
        Task<PoolInfoDto?> GetPoolInfoAsync(string _pool_bech32);
        Task<DelegationResponseDto?> GetPoolDelegationAsync(string poolId, int page = 1, int pageSize = 20, string? sortBy = null, string? sortOrder = null);
    }
}