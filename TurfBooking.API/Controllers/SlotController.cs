using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Application.Common.Result;
using Application.Features.Slot.Queries;
using MediatR;
using System.Threading;

namespace TurfBooking.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class SlotController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SlotController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET /api/slot?turfId=11
        // Shows all available (not booked) slots for a turf
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(
            int turfId, CancellationToken ct = default)
        {
            var result = await _mediator.Send(new GetAvailableSlotsQuery(turfId), ct);
            if (!result.IsSuccess)
                return StatusCode(400, ApiResponse<object>.FailureResponse(result.Error, null, 400));
            return Ok(ApiResponse<object>.SuccessResponse(result.Value, "Slots retrieved successfully"));
        }
    }
}
