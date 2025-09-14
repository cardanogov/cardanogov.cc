using MainAPI.Application.Queries.Pool;
using MainAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs;

namespace MainAPI.Controllers
{
    [ApiController]
    [Route("api/")]
    public class PoolController : BaseController
    {
        private readonly IMediator _mediator;

        public PoolController(IMediator mediator, ILogger<PoolController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpGet("total_pool")]
        public async Task<ActionResult<ApiResponse<int?>>> GetTotalPool()
        {
            try
            {
                var query = new GetTotalPoolQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<int?>($"Error retrieving total pool: {ex.Message}");
            }
        }

        [HttpGet("totals/{epoch_no}")]
        public async Task<ActionResult<ApiResponse<List<TotalInfoResponseDto>>>> GetTotals(int epoch_no)
        {
            try
            {
                var query = new GetTotalsQuery(epoch_no);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<List<TotalInfoResponseDto>>($"Error retrieving totals: {ex.Message}");
            }
        }

        [HttpGet("pool_metadata/{_pool_bech32}")]
        public async Task<ActionResult<ApiResponse<object>>> PostPoolMetadata(string _pool_bech32)
        {
            try
            {
                var query = new GetPoolMetadataQuery(_pool_bech32);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<object>($"Error posting pool metadata: {ex.Message}");
            }
        }

        [HttpGet("pool_stake_snapshot/{_pool_bech32}")]
        public async Task<ActionResult<ApiResponse<object>>> GetPoolStakeSnapshot(string _pool_bech32)
        {
            try
            {
                var query = new GetPoolStakeSnapshotQuery(_pool_bech32);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<object>($"Error retrieving pool stake snapshot: {ex.Message}");
            }
        }

        [HttpGet("spo_voting_power_history")]
        public async Task<ActionResult<ApiResponse<List<SpoVotingPowerHistoryResponseDto>>>> GetSpoVotingPowerHistory()
        {
            try
            {
                var query = new GetSpoVotingPowerHistoryQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<List<SpoVotingPowerHistoryResponseDto>>($"Error retrieving SPO voting power history: {ex.Message}");
            }
        }

        [HttpGet("ada_statistics")]
        public async Task<ActionResult<ApiResponse<AdaStatisticsResponseDto>>> GetAdaStatistics()
        {
            try
            {
                var query = new GetAdaStatisticsQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<AdaStatisticsResponseDto>($"Error retrieving ADA statistics: {ex.Message}");
            }
        }

        [HttpGet("ada_statistics_percentage")]
        public async Task<ActionResult<ApiResponse<AdaStatisticsPercentageResponseDto>>> GetAdaStatisticsPercentage()
        {
            try
            {
                var query = new GetAdaStatisticsPercentageQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<AdaStatisticsPercentageResponseDto>($"Error retrieving ADA statistics percentage: {ex.Message}");
            }
        }

        [HttpGet("pool_list")]
        public async Task<ActionResult<ApiResponse<PoolResponseDto>>> GetPoolList([FromQuery] int page = 1, [FromQuery] int pageSize = 12, [FromQuery] string? status = null, [FromQuery] string? search = null)
        {
            try
            {
                var query = new GetPoolListQuery(page, pageSize, status, search);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<PoolResponseDto>($"Error retrieving pool list: {ex.Message}");
            }
        }

        [HttpGet("pool_info/{_pool_bech32}")]
        public async Task<ActionResult<ApiResponse<PoolInfoDto>>> GetPoolInfo(string _pool_bech32)
        {
            try
            {
                var query = new GetPoolInfoQuery(_pool_bech32);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<PoolInfoDto>($"Error retrieving pool info: {ex.Message}");
            }
        }

        [HttpGet("pool_delegation/{_pool_bech32}")]
        public async Task<ActionResult<ApiResponse<DelegationResponseDto>>> GetPoolDelegation(
            string _pool_bech32,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = null)
        {
            try
            {
                var query = new GetPoolDelegationQuery(_pool_bech32, page, pageSize, sortBy, sortOrder);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<DelegationResponseDto>($"Error retrieving pool delegation: {ex.Message}");
            }
        }
    }
}