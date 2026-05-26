using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Application.Common.Result;
using Application.Features.Booking.Commands;
using Application.Features.Booking.Queries;
using MediatR;
using System.Security.Claims;
using System.Diagnostics;

namespace TurfBooking.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class BookingController : ControllerBase
{
    private static readonly ActivitySource _activity = new("TurfBooking.API");
    private readonly IMediator _mediator;

    public BookingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── POST /api/booking ─────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> BookSlot(
        CreateBookingDto dto,
        CancellationToken ct = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));
        
        var userId = int.Parse(userIdClaim);

        using var span = _activity.StartActivity("BookSlot");
        span?.SetTag("slotId", dto.SlotId);
        span?.SetTag("userId", userId);

        var result = await _mediator.Send(new BookSlotCommand(dto, userId), ct);
        if (!result.IsSuccess)
        {
            var statusCode = result.Error == "Slot not found" ? 404 : 400;
            return StatusCode(statusCode, ApiResponse<object>.FailureResponse(result.Error, null, statusCode));
        }
        return Ok(ApiResponse<object>.SuccessResponse(result.Value, "Slot booked successfully"));
    }

    // ── GET /api/booking/my ───────────────────────────────
    [HttpGet("my")]
    public async Task<IActionResult> MyBookings(CancellationToken ct = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));

        var userId = int.Parse(userIdClaim);
        var result = await _mediator.Send(new GetMyBookingsQuery(userId), ct);
        if (!result.IsSuccess)
            return StatusCode(400, ApiResponse<object>.FailureResponse(result.Error, null, 400));
            
        return Ok(ApiResponse<object>.SuccessResponse(result.Value, "Bookings retrieved successfully"));
    }

    // ── DELETE /api/booking/{id} ───────────────────────────
    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(int id, [FromQuery] string reason, CancellationToken ct = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));

        var userId = int.Parse(userIdClaim);
        var result = await _mediator.Send(new CancelBookingCommand(id, userId, reason), ct);
        if (!result.IsSuccess)
        {
            var statusCode = result.Error == "Booking not found" ? 404 : 400;
            return StatusCode(statusCode, ApiResponse<object>.FailureResponse(result.Error, null, statusCode));
        }

        return Ok(ApiResponse<string>.SuccessResponse(string.Empty, "Booking cancelled successfully"));
    }
}