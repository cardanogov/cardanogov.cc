using SharedLibrary.DTOs;
namespace SharedLibrary.Interfaces
{
    public interface IProposalService
    {
        // Legacy methods - maintained for backward compatibility
        Task<GovernanceActionResponseDto?> GetProposalExpiredAsync();
        Task<List<ProposalInfoResponseDto>?> GetProposalLiveAsync();
        Task<List<ProposalInfoResponseDto>?> GetProposalLiveByIdAsync(string? proposal_id);
        Task<GovernanceActionResponseDto?> GetProposalExpiredByIdAsync(string? proposal_id);

        // New consolidated methods
        Task<GovernanceActionResponseDto?> GetProposalsAsync(bool? isLive);
        Task<GovernanceActionResponseDto?> GetProposalDetailAsync(string? proposalId, bool? isLive);
        Task<ProposalStatsResponseDto?> GetProposalStatsAsync();
        Task<ProposalVotingSummaryResponseDto?> GetProposalVotingSummaryAsync(string gov_id);
        Task<ProposalVotesResponseDto?> GetProposalVotesAsync(string proposal_id, int page, string? filter, string? search);
        Task<GovernanceActionsStatisticsResponseDto?> GetGovernanceActionsStatisticsAsync();
        Task<GovernanceActionsStatisticsByEpochResponseDto?> GetGovernanceActionsStatisticsByEpochAsync();
        Task<List<ProposalActionTypeResponseDto>?> GetProposalActionTypeAsync();
    }
}