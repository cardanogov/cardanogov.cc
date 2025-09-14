using MainAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs;

namespace MainAPI.Controllers
{
    [ApiController]
    [Route("api/")]
    public class TreasuryController : BaseController
    {
        private readonly IMediator _mediator;

        public TreasuryController(IMediator mediator, ILogger<TreasuryController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpGet("total_treasury")]
        public async Task<ActionResult<ApiResponse<TreasuryDataResponseDto>>> GetTotalTreasury()
        {
            try
            {
                var result = await _mediator.Send(new MainAPI.Application.Queries.Treasury.GetTotalTreasuryQuery());
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<TreasuryDataResponseDto>($"Error retrieving total treasury: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("treasury_volatility")]
        public async Task<ActionResult<ApiResponse<TreasuryResponseDto>>> GetTreasuryVolatility()
        {
            try
            {
                var result = await _mediator.Send(new MainAPI.Application.Queries.Treasury.GetTreasuryVolatilityQuery());
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<TreasuryResponseDto>($"Error retrieving treasury volatility: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("treasury_withdrawals")]
        public async Task<ActionResult<ApiResponse<List<TreasuryWithdrawalsResponseDto>>>> GetTreasuryWithdrawals()
        {
            try
            {
                var result = await _mediator.Send(new MainAPI.Application.Queries.Treasury.GetTreasuryWithdrawalsQuery());
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<List<TreasuryWithdrawalsResponseDto>>($"Error retrieving treasury withdrawals: {ex.Message}", statusCode: 500);
            }
        }
    }
}