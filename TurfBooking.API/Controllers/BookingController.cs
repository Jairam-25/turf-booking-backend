using Application.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Application.Common.Result;
using System.Security.Claims;

namespace TurfBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]              // ← JWT token required for all endpoints
public class BookingController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public BookingController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ── POST /api/booking ─────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> BookSlot(
        CreateBookingDto dto)
    {
        // Step 1 : Get logged-in user Id from JWT token
        var userIdClaim = User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim == null)
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));

        var userId = int.Parse(userIdClaim);

        // Step 2 : Check if slot exists
        var slot = await _unitOfWork.Slots.AsQueryable()
            .Include(s => s.Turf)
            .FirstOrDefaultAsync(s => s.Id == dto.SlotId);

        if (slot == null)
            return NotFound(ApiResponse<object>.FailureResponse("Slot not found", null, 404));

        // Step 3 : Check if slot is already booked
        if (slot.IsBooked)
            return BadRequest(ApiResponse<object>.FailureResponse("Slot is already booked", null, 400));

        // Step 4 : Create the booking
        var booking = new Booking
        {
            UserId = userId,
            SlotId = dto.SlotId,
            BookingDate = DateTime.UtcNow
        };

        // Step 5 : Mark slot as booked
        slot.IsBooked = true;

        await _unitOfWork.Bookings.AddAsync(booking);

        await _unitOfWork.SaveChangesAsync();

        var data = new
        {
            bookingId = booking.Id,
            slotId = slot.Id,
            turfName = slot.Turf!.Name,
            location = slot.Turf.Location,
            startTime = slot.StartTime,
            endTime = slot.EndTime,
            bookedOn = booking.BookingDate
        };

        return Ok(ApiResponse<object>.SuccessResponse(data, "Slot booked successfully"));
    }

    // ── GET /api/booking/my ───────────────────────────────
    // View all bookings of logged-in user
    [HttpGet("my")]
    public async Task<IActionResult> MyBookings()
    {
        var userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var bookings = await _unitOfWork.Bookings.AsQueryable()
            .Include(b => b.Slot)
                .ThenInclude(s => s!.Turf)
            .Where(b => b.UserId == userId)
            .Select(b => new
            {
                bookingId = b.Id,
                bookedOn = b.BookingDate,
                turfName = b.Slot!.Turf!.Name,
                location = b.Slot.Turf.Location,
                price = b.Slot.Turf.PricePerHour,
                startTime = b.Slot.StartTime,
                endTime = b.Slot.EndTime
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.SuccessResponse(bookings, "Bookings retrieved successfully"));
    }

    // ── DELETE /api/booking/{id}/cancel ───────────────────
    // Cancel a booking — frees the slot
    [HttpDelete("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var booking = await _unitOfWork.Bookings.AsQueryable()
            .Include(b => b.Slot)
            .FirstOrDefaultAsync(
                b => b.Id == id && b.UserId == userId);

        if (booking == null)
            return NotFound(ApiResponse<object>.FailureResponse("Booking not found", null, 404));

        // Free the slot so others can book it
        booking.Slot!.IsBooked = false;

        await _unitOfWork.Bookings.Delete(booking);

        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResponse(null, "Booking cancelled successfully"));
    }
}