using SharedLibrary.DTOs;

namespace SharedLibrary.Interfaces
{
    public interface IDrepService
    {
        Task<int?> GetTotalDrepAsync();
        Task<TotalDrepResponseDto?> GetTotalStakeNumbersAsync();
        Task<double> GetDrepEpochSummaryAsync(int epoch_no);
        Task<DrepInfoResponseDto?> GetDrepInfoAsync(string drep_id);
        Task<DrepMetadataResponseDto?> GetDrepMetadataAsync(string drep_id);
        Task<List<DrepDelegatorsResponseDto>?> GetDrepDelegatorsAsync(string drep_id);
        Task<DrepHistoryResponseDto?> GetDrepHistoryAsync(int epoch_no, string drep_id);
        Task<List<DrepsUpdatesResponseDto>?> GetDrepUpdatesAsync(string drep_id);
        Task<DrepVoteInfoResponseDto?> GetDrepVotesAsync(string drep_id);
        Task<List<DrepVotingPowerHistoryResponseDto>?> GetDrepVotingPowerHistoryAsync();
        Task<List<DrepVotingPowerHistoryResponseDto>?> GetTop10DrepVotingPowerAsync();
        Task<TotalWalletStatisticsResponseDto?> GetTotalWalletStatisticsAsync();
        Task<DrepPoolVotingThresholdResponseDto?> GetDrepAndPoolVotingThresholdAsync();
        Task<DrepPoolStakeThresholdResponseDto?> GetDrepTotalStakeApprovalThresholdAsync(int epoch_no, string proposal_type);
        Task<DrepCardDataResponseDto?> GetDrepCardDataAsync();
        Task<DrepCardDataByIdResponseDto?> GetDrepCardDataByIdAsync(string drep_id);
        Task<List<DrepVoteInfoResponseDto>?> GetDrepVoteInfoAsync(string drep_id);
        Task<DrepDelegationResponseDto?> GetDrepDelegationAsync(string drep_id);
        Task<List<DrepRegistrationTableResponseDto>?> GetDrepRegistrationAsync(string drep_id);
        Task<List<DrepDetailsVotingPowerResponseDto>?> GetDrepDetailsVotingPowerAsync(string drep_id);
        Task<DrepListResponseDto?> GetDrepListAsync(int page, string? search, string? status);
        Task<DrepsVotingPowerResponseDto?> GetDrepsVotingPowerAsync();
        Task<List<DrepNewRegisterResponseDto>?> GetDrepNewRegisterAsync();
    }
}