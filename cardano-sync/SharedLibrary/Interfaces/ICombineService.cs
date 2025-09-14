using SharedLibrary.DTOs;

namespace SharedLibrary.Interfaces
{
    public interface ICombineService
    {
        Task<MembershipDataResponseDto?> GetTotalsMembershipAsync();
        Task<ParticipateInVotingResponseDto?> GetParticipateInVotingAsync();
        Task<List<GovernanceParametersResponseDto>> GetGovernanceParametersAsync();
        Task<AllocationResponseDto?> GetAllocationAsync();
        Task<SearchApiResponseDto?> GetSearchAsync(string? searchTerm);
    }
}