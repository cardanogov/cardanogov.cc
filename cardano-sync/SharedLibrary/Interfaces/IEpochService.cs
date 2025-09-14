using SharedLibrary.DTOs;

namespace SharedLibrary.Interfaces
{
    public interface IEpochService
    {
        Task<EpochInfoResponseDto?> GetEpochInfoAsync(int epoch_no);
        Task<List<CurrentEpochResponseDto>?> GetCurrentEpochAsync();
        Task<TotalStakeResponseDto?> GetTotalStakeAsync();
        Task<EpochInfoResponseDto?> GetCurrentEpochInfoAsync();
        Task<int?> GetEpochInfoSpoAsync(int epoch_no);
    }
}