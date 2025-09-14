using MainAPI.Application.Queries.Combine;
using MainAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs;

namespace MainAPI.Controllers
{
    [ApiController]
    [Route("api/")]
    public class CombineController : BaseController
    {
        private readonly IMediator _mediator;

        public CombineController(IMediator mediator, ILogger<CombineController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpGet("totals_membership")]
        public async Task<ActionResult<ApiResponse<MembershipDataResponseDto>>> GetTotalsMembership()
        {
            try
            {
                var result = await _mediator.Send(new GetTotalMemberShipQuery());
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<MembershipDataResponseDto>($"Error retrieving totals membership: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("participate_in_voting")]
        public async Task<ActionResult<ApiResponse<ParticipateInVotingResponseDto>>> GetParticipateInVoting()
        {
            try
            {
                var result = await _mediator.Send(new GetParticipateInVotingQuery());
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<ParticipateInVotingResponseDto>($"Error retrieving participate in voting: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("governance_parameters")]
        public async Task<ActionResult<ApiResponse<List<GovernanceParametersResponseDto>>>> GetGovernanceParameters()
        {
            try
            {
                var result = await _mediator.Send(new GetGovernanceParametersQuery());
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<List<GovernanceParametersResponseDto>>($"Error retrieving governance parameters: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("allocation")]
        public async Task<ActionResult<ApiResponse<AllocationResponseDto>>> GetAllocation()
        {
            try
            {
                var result = await _mediator.Send(new GetAllocationQuery());
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<AllocationResponseDto>($"Error retrieving allocation: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<SearchApiResponseDto>>> GetSearch([FromQuery] string? searchTerm)
        {
            try
            {
                var result = await _mediator.Send(new GetSearchQuery(searchTerm));
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<SearchApiResponseDto>($"Error retrieving search results: {ex.Message}", statusCode: 500);
            }
        }
    }
}