using MainAPI.Application.Queries.StakeAddresses;
using MainAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MainAPI.Controllers
{
    [ApiController]
    [Route("api/")]
    public class AccountController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IMediator mediator, ILogger<AccountController> logger) : base(logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet("total_stake_addresses")]
        public async Task<IActionResult> GetTotalStakeAddresses()
        {
            try
            {
                _logger.LogInformation("Getting total stake addresses");

                var query = new GetTotalStakeAddressesQuery();
                var totalCount = await _mediator.Send(query);

                return Ok(
                    new ApiResponse<int>
                    {
                        Success = true,
                        Data = totalCount,
                        Message = $"Total stake addresses retrieved successfully: {totalCount:N0}",
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total stake addresses");
                return StatusCode(
                    500,
                    new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Internal server error while retrieving total stake addresses",
                    }
                );
            }
        }
    }
}
