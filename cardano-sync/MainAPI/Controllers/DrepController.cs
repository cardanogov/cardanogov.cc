using MainAPI.Application.Queries.Drep;
using MainAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs;

namespace MainAPI.Controllers
{
    [ApiController]
    [Route("api/")]
    public class DrepController : BaseController
    {
        private readonly IMediator _mediator;

        public DrepController(IMediator mediator, ILogger<DrepController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpGet("total_drep")]
        public async Task<ActionResult<ApiResponse<int?>>> GetTotalDrep()
        {
            try
            {
                var query = new GetTotalDrepQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<int?>($"Error retrieving total drep: {ex.Message}");
            }
        }

        [HttpGet("total_stake_numbers")]
        public async Task<ActionResult<ApiResponse<TotalDrepResponseDto?>>> GetTotalStakeNumbers()
        {
            try
            {
                var query = new GetTotalStakeNumbersQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<TotalDrepResponseDto?>($"Error retrieving total stake numbers: {ex.Message}");
            }
        }

        [HttpGet("drep_epoch_summary/{epoch_no}")]
        public async Task<ActionResult<ApiResponse<double>>> GetDrepEpochSummary(int epoch_no)
        {
            try
            {
                var query = new GetDrepEpochSummaryQuery(epoch_no);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<double>($"Error retrieving drep epoch summary: {ex.Message}");
            }
        }

        [HttpGet("drep_info/{drep_id}")]
        public async Task<ActionResult<ApiResponse<DrepInfoResponseDto?>>> GetDrepInfo(string drep_id)
        {
            try
            {
                var query = new GetDrepInfoQuery(drep_id);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<DrepInfoResponseDto?>($"Error retrieving drep info: {ex.Message}");
            }
        }

        [HttpGet("drep_metadata/{drep_id}")]
        public async Task<ActionResult<ApiResponse<DrepMetadataResponseDto?>>> GetDrepMetadata(string drep_id)
        {
            try
            {
                var query = new GetDrepMetadataQuery(drep_id);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<DrepMetadataResponseDto?>($"Error posting drep metadata: {ex.Message}");
            }
        }

        [HttpGet("drep_delegators/{drep_id}")]
        public async Task<ActionResult<ApiResponse<List<DrepDelegatorsResponseDto>?>>> GetDrepDelegators(string drep_id)
        {
            try
            {
                var query = new GetDrepDelegatorsQuery(drep_id);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<List<DrepDelegatorsResponseDto>?>($"Error retrieving drep delegators: {ex.Message}");
            }
        }

        [HttpGet("drep_history/{epoch_no}/{drep_id}")]
        public async Task<ActionResult<ApiResponse<DrepHistoryResponseDto?>>> GetDrepHistory(int epoch_no, string drep_id)
        {
            try
            {
                var query = new GetDrepHistoryQuery(epoch_no, drep_id);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<DrepHistoryResponseDto?>($"Error retrieving drep history: {ex.Message}");
            }
        }

        [HttpGet("drep_updates/{drep_id}")]
        public async Task<ActionResult<ApiResponse<List<DrepsUpdatesResponseDto>?>>> GetDrepUpdates(string drep_id)
        {
            try
            {
                var query = new GetDrepUpdatesQuery(drep_id);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<List<DrepsUpdatesResponseDto>?>($"Error retrieving drep updates: {ex.Message}");
            }
        }

        [HttpGet("drep_votes/{drep_id}")]
        public async Task<ActionResult<ApiResponse<DrepVoteInfoResponseDto?>>> GetDrepVotes(string drep_id)
        {
            try
            {
                var query = new GetDrepVotesQuery(drep_id);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<DrepVoteInfoResponseDto?>($"Error retrieving drep votes: {ex.Message}");
            }
        }

        [HttpGet("drep_voting_power_history")]
        public async Task<ActionResult<ApiResponse<List<DrepVotingPowerHistoryResponseDto>?>>> GetDrepVotingPowerHistory()
        {
            try
            {
                var query = new GetDrepVotingPowerHistoryQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<List<DrepVotingPowerHistoryResponseDto>?>($"Error retrieving drep voting power history: {ex.Message}");
            }
        }

        [HttpGet("drep_list")]
        public async Task<ActionResult<ApiResponse<DrepListResponseDto?>>> GetDrepList([FromQuery] int page = 1, [FromQuery] string? search = null, [FromQuery] string? status = null)
        {
            try
            {
                var query = new GetDrepListQuery(page, search, status);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<DrepListResponseDto?>($"Error retrieving drep list: {ex.Message}");
            }
        }

        [HttpGet("top_10_drep_voting_power")]
        public async Task<ActionResult<ApiResponse<List<DrepVotingPowerHistoryResponseDto>?>>> GetTop10DrepVotingPower()
        {
            try
            {
                var query = new GetTop10DrepVotingPowerQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<List<DrepVotingPowerHistoryResponseDto>?>($"Error retrieving top 10 drep voting power: {ex.Message}");
            }
        }

        [HttpGet("total_wallet_stastics")]
        public async Task<ActionResult<ApiResponse<TotalWalletStatisticsResponseDto?>>> GetTotalWalletStatistics()
        {
            try
            {
                var query = new GetTotalWalletStatisticsQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<TotalWalletStatisticsResponseDto?>($"Error retrieving total wallet statistics: {ex.Message}");
            }
        }

        [HttpGet("drep_and_pool_voting_threshold")]
        public async Task<ActionResult<ApiResponse<DrepPoolVotingThresholdResponseDto?>>> GetDrepAndPoolVotingThreshold()
        {
            try
            {
                var query = new GetDrepAndPoolVotingThresholdQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<DrepPoolVotingThresholdResponseDto?>($"Error retrieving drep and pool voting threshold: {ex.Message}");
            }
        }

        [HttpGet("drep_total_stake_approval_threshold/{epoch_no}/{proposal_type}")]
        public async Task<ActionResult<ApiResponse<DrepPoolStakeThresholdResponseDto?>>> GetDrepTotalStakeApprovalThreshold(int epoch_no, string proposal_type)
        {
            try
            {
                var query = new GetDrepTotalStakeApprovalThresholdQuery(epoch_no, proposal_type);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<DrepPoolStakeThresholdResponseDto?>($"Error retrieving drep total stake approval threshold: {ex.Message}");
            }
        }

        [HttpGet("drep_card_data")]
        public async Task<ActionResult<ApiResponse<DrepCardDataResponseDto?>>> GetDrepCardData()
        {
            try
            {
                var query = new GetDrepCardDataQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<DrepCardDataResponseDto?>($"Error retrieving drep card data: {ex.Message}");
            }
        }

        [HttpGet("drep_card_data_by_id/{drep_id}")]
        public async Task<ActionResult<ApiResponse<DrepCardDataByIdResponseDto?>>> GetDrepCardDataById(string drep_id)
        {
            try
            {
                var query = new GetDrepCardDataByIdQuery(drep_id);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<DrepCardDataByIdResponseDto?>($"Error retrieving drep card data by id: {ex.Message}");
            }
        }

        [HttpGet("drep_vote_info/{drep_id}")]
        public async Task<ActionResult<ApiResponse<List<DrepVoteInfoResponseDto>?>>> GetDrepVoteInfo(string drep_id)
        {
            try
            {
                var query = new GetDrepVoteInfoQuery(drep_id);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<List<DrepVoteInfoResponseDto>?>($"Error retrieving drep vote info: {ex.Message}");
            }
        }

        [HttpGet("drep_delegation/{drep_id}")]
        public async Task<ActionResult<ApiResponse<DrepDelegationResponseDto?>>> GetDrepDelegation(string drep_id)
        {
            try
            {
                var query = new GetDrepDelegationQuery(drep_id);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<DrepDelegationResponseDto?>($"Error retrieving drep delegation: {ex.Message}");
            }
        }

        [HttpGet("drep_registration/{drep_id}")]
        public async Task<ActionResult<ApiResponse<List<DrepRegistrationTableResponseDto>?>>> GetDrepRegistration(string drep_id)
        {
            try
            {
                var query = new GetDrepRegistrationQuery(drep_id);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<List<DrepRegistrationTableResponseDto>?>($"Error retrieving drep registration: {ex.Message}");
            }
        }

        [HttpGet("drep_details_voting_power/{drep_id}")]
        public async Task<ActionResult<ApiResponse<List<DrepDetailsVotingPowerResponseDto>?>>> GetDrepDetailsVotingPower(string drep_id)
        {
            try
            {
                var query = new GetDrepDetailsVotingPowerQuery(drep_id);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<List<DrepDetailsVotingPowerResponseDto>?>($"Error retrieving drep details voting power: {ex.Message}");
            }
        }

        [HttpGet("dreps_voting_power")]
        public async Task<ActionResult<ApiResponse<DrepsVotingPowerResponseDto?>>> GetDrepsVotingPower()
        {
            try
            {
                var query = new GetDrepsVotingPowerQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<DrepsVotingPowerResponseDto?>($"Error retrieving dreps voting power: {ex.Message}");
            }
        }

        [HttpGet("dreps_new_register")]
        public async Task<ActionResult<ApiResponse<List<DrepNewRegisterResponseDto>?>>> GetDrepNewRegister()
        {
            try
            {
                var query = new GetDrepNewRegisterQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<List<DrepNewRegisterResponseDto>?>($"Error retrieving drep new register: {ex.Message}");
            }
        }
    }
}