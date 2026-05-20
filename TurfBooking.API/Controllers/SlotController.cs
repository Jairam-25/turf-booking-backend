using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Asp.Versioning;
using Application.Common.Result;

namespace TurfBooking.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class SlotController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public SlotController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET /api/slot?turfId=11
        // Shows all available (not booked) slots for a turf
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(
            int turfId)
        {
            var slots = await _unitOfWork.Slots.AsQueryable()
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

            return Ok(ApiResponse<object>.SuccessResponse(slots, "Slots retrieved successfully"));
        }
    }

}
