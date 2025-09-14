using MainAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs;

namespace MainAPI.Controllers
{
    [ApiController]
    [Route("api/")]
    public class VotingController : BaseController
    {
        private readonly IMediator _mediator;

        public VotingController(IMediator mediator, ILogger<VotingController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpGet("voting_cards_data")]
        public async Task<ActionResult<ApiResponse<VotingCardInfoDto>>> GetVotingCardData()
        {
            try
            {
                var result = await _mediator.Send(new MainAPI.Application.Queries.Voting.GetVotingCardDataQuery());
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<VotingCardInfoDto>($"Error retrieving voting cards data: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("voting_history")]
        public async Task<ActionResult<ApiResponse<VotingHistoryResponseDto>>> GetVotingHistory([FromQuery] int page = 1, [FromQuery] string? filter = null, [FromQuery] string? search = null)
        {
            try
            {
                var query = new MainAPI.Application.Queries.Voting.GetVotingHistoryQuery(page, filter, search);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                if (ex.Message == "No voting history data available")
                {
                    return NotFound<VotingHistoryResponseDto>("No voting history data available");
                }
                return Error<VotingHistoryResponseDto>($"Error retrieving voting history: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("vote_list")]
        public async Task<ActionResult<ApiResponse<List<VoteListResponseDto>>>> GetVoteList()
        {
            try
            {
                var result = await _mediator.Send(new MainAPI.Application.Queries.Voting.GetVoteListQuery());
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<List<VoteListResponseDto>>($"Error retrieving vote list: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("vote_statistic_drep_spo")]
        public async Task<ActionResult<ApiResponse<List<VoteStatisticResponseDto>>>> GetVoteStatisticDrepSpo()
        {
            try
            {
                var result = await _mediator.Send(new MainAPI.Application.Queries.Voting.GetVoteStatisticDrepSpoQuery());
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<List<VoteStatisticResponseDto>>($"Error retrieving vote statistic drep spo: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("vote_participation_index")]
        public async Task<ActionResult<ApiResponse<int?>>> GetVoteParticipationIndex()
        {
            try
            {
                var result = await _mediator.Send(new MainAPI.Application.Queries.Voting.GetVoteParticipationIndexQuery());
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<int?>($"Error retrieving vote participation index: {ex.Message}", statusCode: 500);
            }
        }
    }
}