using Application.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]              // ← JWT token required for all endpoints
public class BookingController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BookingController(ApplicationDbContext context)
    {
        _context = context;
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
            return Unauthorized(
                new { message = "Invalid token" });

        var userId = int.Parse(userIdClaim);

        // Step 2 : Check if slot exists
        var slot = await _context.Slots
            .Include(s => s.Turf)
            .FirstOrDefaultAsync(s => s.Id == dto.SlotId);

        if (slot == null)
            return NotFound(
                new { message = "Slot not found" });

        // Step 3 : Check if slot is already booked
        if (slot.IsBooked)
            return BadRequest(
                new { message = "Slot is already booked" });

        // Step 4 : Create the booking
        var booking = new Booking
        {
            UserId = userId,
            SlotId = dto.SlotId,
            BookingDate = DateTime.UtcNow
        };

        // Step 5 : Mark slot as booked
        slot.IsBooked = true;

        _context.Bookings.Add(booking);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Slot booked successfully",
            bookingId = booking.Id,
            slotId = slot.Id,
            turfName = slot.Turf!.Name,
            location = slot.Turf.Location,
            startTime = slot.StartTime,
            endTime = slot.EndTime,
            bookedOn = booking.BookingDate
        });
    }

    // ── GET /api/booking/my ───────────────────────────────
    // View all bookings of logged-in user
    [HttpGet("my")]
    public async Task<IActionResult> MyBookings()
    {
        var userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var bookings = await _context.Bookings
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

        return Ok(bookings);
    }

    // ── DELETE /api/booking/{id}/cancel ───────────────────
    // Cancel a booking — frees the slot
    [HttpDelete("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var booking = await _context.Bookings
            .Include(b => b.Slot)
            .FirstOrDefaultAsync(
                b => b.Id == id && b.UserId == userId);

        if (booking == null)
            return NotFound(
                new { message = "Booking not found" });

        // Free the slot so others can book it
        booking.Slot!.IsBooked = false;

        _context.Bookings.Remove(booking);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Booking cancelled successfully" });
    }
}