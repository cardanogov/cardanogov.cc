using SharedLibrary.DTOs;

namespace SharedLibrary.Interfaces
{
    public interface IVotingService
    {
        Task<VotingCardInfoDto?> GetVotingCardDataAsync();
        Task<VotingHistoryResponseDto?> GetVotingHistoryAsync(int page, string? filter, string? search);
        Task<List<VoteListResponseDto>?> GetVoteListAsync();
        Task<List<VoteStatisticResponseDto>?> GetVoteStatisticDrepSpoAsync();
        Task<int?> GetVoteParticipationIndexAsync();
    }
}