using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Persistence.Context;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace TurfBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TestController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("test-query")]
    public async Task<IActionResult> TestQuery()
    {
        var bookings = await _context.Bookings.AsQueryable()
            .Include(b => b.Slot)
            .ThenInclude(s => s!.Turf)
            .OrderByDescending(b => b.BookingDate)
            .Select(b => new
            {
                bookingId = b.Id,
                bookedOn = b.BookingDate,
                turfName = (string?)b.Slot!.Turf!.Name ?? "Unknown Turf",
                location = (string?)b.Slot!.Turf!.Location ?? "Unknown Location",
                price = (decimal?)b.Slot!.Turf!.PricePerHour ?? 0m,
                startTime = (System.DateTime?)b.Slot!.StartTime,
                endTime = (System.DateTime?)b.Slot!.EndTime
            })
            .ToListAsync();
        return Ok(bookings);
    }
}
