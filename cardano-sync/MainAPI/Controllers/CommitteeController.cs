using MainAPI.Application.Queries.Committee;
using MainAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs;

namespace MainAPI.Controllers
{
    [ApiController]
    [Route("api/")]
    public class CommitteeController : BaseController
    {
        private readonly IMediator _mediator;

        public CommitteeController(IMediator mediator, ILogger<CommitteeController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpGet("total_committee")]
        public async Task<ActionResult<ApiResponse<int?>>> GetTotalCommittee()
        {
            try
            {
                var query = new GetTotalCommitteeQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<int?>($"Error retrieving total committee: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("committee_votes/{cc_hot_id}")]
        public async Task<ActionResult<ApiResponse<List<CommitteeVotesResponseDto>>>> GetCommitteeVotes(string cc_hot_id)
        {
            try
            {
                var query = new GetCommitteeVotesQuery(cc_hot_id);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<List<CommitteeVotesResponseDto>>($"Error retrieving committee votes: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("committee_info")]
        public async Task<ActionResult<ApiResponse<List<CommitteeInfoResponseDto>>>> GetCommitteeInfo()
        {
            try
            {
                var query = new GetCommitteeInfoQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<List<CommitteeInfoResponseDto>>($"Error retrieving committee info: {ex.Message}", statusCode: 500);
            }
        }
    }
}