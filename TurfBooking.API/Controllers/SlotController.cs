using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace TurfBooking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SlotController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SlotController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/slot?turfId=11
        // Shows all available (not booked) slots for a turf
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(
            int turfId)
        {
            var slots = await _context.Slots
                .Where(s => s.TurfId == turfId
                            && !s.IsBooked)
                .Select(s => new
                {
                    slotId = s.Id,
                    startTime = s.StartTime,
                    endTime = s.EndTime,
                    turfId = s.TurfId
                })
                .ToListAsync();

            return Ok(slots);
        }
    }

}
