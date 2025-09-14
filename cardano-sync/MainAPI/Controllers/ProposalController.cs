using MainAPI.Application.Queries.Proposal;
using MainAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs;

namespace MainAPI.Controllers
{
    [ApiController]
    [Route("api/")]
    public class ProposalController : BaseController
    {
        private readonly IMediator _mediator;

        public ProposalController(IMediator mediator, ILogger<ProposalController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        // Legacy endpoints - maintained for backward compatibility
        [HttpGet("proposal_expired")]
        public async Task<ActionResult<ApiResponse<GovernanceActionResponseDto>>> GetProposalExpired()
        {
            try
            {
                var query = new GetProposalsQuery(isLive: false);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<GovernanceActionResponseDto>($"Error retrieving proposal expired: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("proposal_live")]
        public async Task<ActionResult<ApiResponse<List<ProposalInfoResponseDto>>>> GetProposalLive()
        {
            try
            {
                var query = new GetProposalsQuery(isLive: true);
                var result = await _mediator.Send(query);
                // Convert GovernanceActionResponseDto to List<ProposalInfoResponseDto> for backward compatibility
                return Success(result?.proposal_info ?? new List<ProposalInfoResponseDto>());
            }
            catch (Exception ex)
            {
                return Error<List<ProposalInfoResponseDto>>($"Error retrieving proposal live: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("proposal_live_detail/{proposal_id?}")]
        public async Task<ActionResult<ApiResponse<List<ProposalInfoResponseDto>>>> GetProposalLiveDetail(string? proposal_id = null)
        {
            try
            {
                var query = new GetProposalDetailQuery(proposal_id, isLive: true);
                var result = await _mediator.Send(query);
                // Convert GovernanceActionResponseDto to List<ProposalInfoResponseDto> for backward compatibility
                return Success(result?.proposal_info ?? new List<ProposalInfoResponseDto>());
            }
            catch (Exception ex)
            {
                return Error<List<ProposalInfoResponseDto>>($"Error retrieving proposal live detail: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("proposal_expired_detail/{proposal_id?}")]
        public async Task<ActionResult<ApiResponse<GovernanceActionResponseDto>>> GetProposalExpiredDetail(string? proposal_id = null)
        {
            try
            {
                var query = new GetProposalDetailQuery(proposal_id, isLive: false);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<GovernanceActionResponseDto>($"Error retrieving proposal expired detail: {ex.Message}", statusCode: 500);
            }
        }

        // New consolidated endpoints
        [HttpGet("proposals")]
        public async Task<ActionResult<ApiResponse<GovernanceActionResponseDto>>> GetProposals([FromQuery] bool? isLive = null)
        {
            try
            {
                var query = new GetProposalsQuery(isLive);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<GovernanceActionResponseDto>($"Error retrieving proposals: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("proposals/{proposal_id?}")]
        public async Task<ActionResult<ApiResponse<GovernanceActionResponseDto>>> GetProposalDetail(string? proposal_id = null, [FromQuery] bool? isLive = null)
        {
            try
            {
                var query = new GetProposalDetailQuery(proposal_id, isLive);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<GovernanceActionResponseDto>($"Error retrieving proposal detail: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("proposal_stats")]
        public async Task<ActionResult<ApiResponse<ProposalStatsResponseDto>>> GetProposalStats()
        {
            try
            {
                var query = new GetProposalStatsQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<ProposalStatsResponseDto>($"Error retrieving proposal stats: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("proposal_voting_summary/{gov_id}")]
        public async Task<ActionResult<ApiResponse<ProposalVotingSummaryResponseDto>>> GetProposalVotingSummary(string gov_id)
        {
            try
            {
                var query = new GetProposalVotingSummaryQuery(gov_id);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<ProposalVotingSummaryResponseDto>($"Error retrieving proposal voting summary: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("proposal_votes/{proposal_id}")]
        public async Task<ActionResult<ApiResponse<ProposalVotesResponseDto>>> GetProposalVotes(string proposal_id, [FromQuery] int page, [FromQuery] string? filter = null, [FromQuery] string? search = null)
        {
            try
            {
                var query = new GetProposalVotesQuery(proposal_id, page, filter, search);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<ProposalVotesResponseDto>($"Error retrieving proposal votes: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("governance_actions_statistics")]
        public async Task<ActionResult<ApiResponse<GovernanceActionsStatisticsResponseDto>>> GetGovernanceActionsStatistics()
        {
            try
            {
                var query = new GetGovernanceActionsStatisticsQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<GovernanceActionsStatisticsResponseDto>($"Error retrieving governance actions statistics: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("governance_actions_statistics_by_epoch")]
        public async Task<ActionResult<ApiResponse<GovernanceActionsStatisticsByEpochResponseDto>>> GetGovernanceActionsStatisticsByEpoch()
        {
            try
            {
                var query = new GetGovernanceActionsStatisticsByEpochQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<GovernanceActionsStatisticsByEpochResponseDto>($"Error retrieving governance actions statistics by epoch: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("proposal_action_type")]
        public async Task<ActionResult<ApiResponse<List<ProposalActionTypeResponseDto>>>> GetProposalActionType()
        {
            try
            {
                var query = new GetProposalActionTypeQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<List<ProposalActionTypeResponseDto>>($"Error retrieving proposal action type: {ex.Message}", statusCode: 500);
            }
        }
    }
}