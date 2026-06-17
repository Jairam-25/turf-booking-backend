using Application.Common.Result;
using Application.Features.Promo.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace TurfBooking.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class PromoOffersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PromoOffersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetPromoOffers(CancellationToken ct)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));

            var userId = int.Parse(userIdClaim);
            var query = new GetPromoOffersQuery { UserId = userId };
            var result = await _mediator.Send(query, ct);

            return Ok(ApiResponse<object>.SuccessResponse(result, "Promo offers retrieved successfully."));
        }

        [HttpGet("validate/{code}")]
        public async Task<IActionResult> ValidatePromoCode(string code, CancellationToken ct)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));

            var userId = int.Parse(userIdClaim);
            var query = new ValidatePromoCodeQuery { UserId = userId, PromoCode = code };
            var result = await _mediator.Send(query, ct);

            if (!result.IsValid)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(result.Message, null, 400));
            }

            return Ok(ApiResponse<object>.SuccessResponse(result, result.Message));
        }
    }
}
