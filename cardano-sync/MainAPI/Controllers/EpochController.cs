using MainAPI.Application.Queries.Epoch;
using MainAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs;

namespace MainAPI.Controllers
{
    [ApiController]
    [Route("api/")]
    public class EpochController : BaseController
    {
        private readonly IMediator _mediator;

        public EpochController(IMediator mediator, ILogger<EpochController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpGet("epoch_info/{epoch_no}")]
        public async Task<ActionResult<ApiResponse<EpochInfoResponseDto>>> GetEpochInfo(int epoch_no, [FromQuery] bool include_next_epoch = false)
        {
            try
            {
                var query = new GetEpochInfoQuery(epoch_no);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<EpochInfoResponseDto>($"Error retrieving epoch info: {ex.Message}");
            }
        }

        [HttpGet("current_epoch")]
        public async Task<ActionResult<ApiResponse<List<CurrentEpochResponseDto>>>> GetCurrentEpoch()
        {
            try
            {
                var query = new GetCurrentEpochQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<List<CurrentEpochResponseDto>>($"Error retrieving current epoch: {ex.Message}");
            }
        }

        [HttpGet("total_stake")]
        public async Task<ActionResult<ApiResponse<TotalStakeResponseDto>>> GetTotalStake()
        {
            try
            {
                var query = new GetTotalStakeQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<TotalStakeResponseDto>($"Error retrieving total stake: {ex.Message}");
            }
        }

        [HttpGet("current_epoch_info")]
        public async Task<ActionResult<ApiResponse<EpochInfoResponseDto>>> GetCurrentEpochInfo()
        {
            try
            {
                var query = new GetCurrentEpochInfoQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<EpochInfoResponseDto>($"Error retrieving current epoch info: {ex.Message}");
            }
        }

        [HttpGet("epoch_info_spo/{epoch_no}")]
        public async Task<ActionResult<ApiResponse<int?>>> GetEpochInfoSpo(int epoch_no)
        {
            try
            {
                var query = new GetEpochInfoSpoQuery(epoch_no);
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<int?>($"Error retrieving epoch info spo: {ex.Message}");
            }
        }
    }
}