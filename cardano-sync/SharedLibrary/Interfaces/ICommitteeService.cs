using SharedLibrary.DTOs;

namespace SharedLibrary.Interfaces
{
    public interface ICommitteeService
    {
        Task<int?> GetTotalCommitteeAsync();
        Task<List<CommitteeVotesResponseDto>?> GetCommitteeVotesAsync(string cc_hot_id);
        Task<List<CommitteeInfoResponseDto>?> GetCommitteeInfoAsync();
    }
}