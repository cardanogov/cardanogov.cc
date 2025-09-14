using MainAPI.Application.Queries.Price;
using MainAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MainAPI.Controllers
{
    [ApiController]
    [Route("api/")]
    public class PriceController : BaseController
    {
        private readonly IMediator _mediator;

        public PriceController(IMediator mediator, ILogger<PriceController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpGet("usd_price")]
        public async Task<ActionResult<ApiResponse<decimal?>>> GetUsdPrice()
        {
            try
            {
                var query = new GetPriceQuery();
                var result = await _mediator.Send(query);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Error<decimal?>($"Error retrieving USD price: {ex.Message}", statusCode: 500);
            }
        }
    }
}