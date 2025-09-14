using SharedLibrary.DTOs;

namespace SharedLibrary.Interfaces
{
    public interface ITreasuryService
    {
        Task<TreasuryDataResponseDto?> GetTotalTreasuryAsync();
        Task<TreasuryResponseDto?> GetTreasuryVolatilityAsync();
        Task<List<TreasuryWithdrawalsResponseDto>?> GetTreasuryWithdrawalsAsync();
    }
}