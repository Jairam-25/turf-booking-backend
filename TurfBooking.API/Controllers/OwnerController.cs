using Application.Common.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;

namespace TurfBooking.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Owner")]
public class OwnerController : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromServices] Application.Interfaces.IUnitOfWork unitOfWork)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));

        var userId = int.Parse(userIdClaim);

        var allTurfs = await unitOfWork.Turfs.GetAllAsync();
        var myTurf = allTurfs.FirstOrDefault(t => t.OwnerId == userId);
        
        if (myTurf == null) 
        {
            return Ok(ApiResponse<object>.SuccessResponse(new { TurfName = "Unassigned Turf", Stats = new { Revenue = 0, Bookings = 0, Utilization = 0, Pending = 0 }, RecentBookings = new object[0] }, "No turf assigned"));
        }

        var allSlots = await unitOfWork.Slots.GetAllAsync();
        var mySlots = allSlots.Where(s => s.TurfId == myTurf.Id).ToList();
        var slotIds = mySlots.Select(s => s.Id).ToHashSet();

        var allBookings = await unitOfWork.Bookings.GetAllAsync();
        var myBookings = allBookings.Where(b => slotIds.Contains(b.SlotId)).ToList();
        
        var allUsers = await unitOfWork.Users.GetAllAsync();

        var recentBookingsDto = myBookings.Select(b => {
            var slot = mySlots.First(s => s.Id == b.SlotId);
            var user = allUsers.FirstOrDefault(u => u.Id == b.UserId);
            
            var displayUser = user?.Name ?? "Unknown User";
            if (b.UserId == myTurf.OwnerId)
            {
                displayUser = "Offline Booking";
            }
            
            var basePrice = myTurf.PricePerHour;
            var hour = slot.StartTime.Hour;
            decimal price = basePrice;
            if (hour >= 6 && hour < 12) price = myTurf.DayTimePrice ?? (basePrice * 0.75m);
            else if (hour >= 12 && hour < 17) price = myTurf.AfternoonPrice ?? basePrice;
            else price = myTurf.NightTimePrice ?? (basePrice * 1.125m);

            return new {
                Id = $"B-{b.Id}",
                User = displayUser,
                Date = slot.StartTime.ToString("yyyy-MM-dd"),
                Time = $"{slot.StartTime:HH:mm} - {slot.EndTime:HH:mm}",
                Amount = Math.Round(price),
                Status = "Confirmed"
            };
        }).OrderByDescending(b => b.Date).ToArray();

        var dashboardData = new
        {
            TurfId = myTurf.Id,
            TurfName = myTurf.Name,
            PricePerHour = myTurf.PricePerHour,
            DayTimePrice = myTurf.DayTimePrice,
            AfternoonPrice = myTurf.AfternoonPrice,
            NightTimePrice = myTurf.NightTimePrice,
            Stats = new
            {
                Revenue = recentBookingsDto.Sum(b => b.Amount),
                Bookings = recentBookingsDto.Length,
                Utilization = mySlots.Count > 0 ? (myBookings.Count * 100 / mySlots.Count) : 0,
                Pending = 0
            },
            RecentBookings = recentBookingsDto
        };

        return Ok(ApiResponse<object>.SuccessResponse(dashboardData, "Owner dashboard retrieved successfully"));
    }

    [HttpPost("settings")]
    public async Task<IActionResult> SaveSettings([FromBody] TurfSettingsDto dto, [FromServices] Application.Interfaces.IUnitOfWork unitOfWork, [FromServices] Microsoft.Extensions.Caching.Distributed.IDistributedCache cache)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));

        var userId = int.Parse(userIdClaim);

        var allTurfs = await unitOfWork.Turfs.GetAllAsync();
        var myTurf = allTurfs.FirstOrDefault(t => t.OwnerId == userId);

        if (myTurf == null)
            return BadRequest(ApiResponse<object>.FailureResponse("No turf assigned to this owner."));

        myTurf.Name = dto.TurfName ?? myTurf.Name;
        if (dto.PricePerHour.HasValue) myTurf.PricePerHour = dto.PricePerHour.Value;
        
        myTurf.DayTimePrice = dto.DayTimePrice;
        myTurf.AfternoonPrice = dto.AfternoonPrice;
        myTurf.NightTimePrice = dto.NightTimePrice;

        await unitOfWork.SaveChangesAsync();
        
        // Invalidate Redis cache for this turf
        try
        {
            await cache.RemoveAsync($"turfs_details_{myTurf.Id}");
            await cache.RemoveAsync($"turfs_p1_ps10_locall_min0_max0_sortid_asc");
            await cache.RemoveAsync($"turfs_p1_ps50_locall_min0_max0_sortid_asc");
            await cache.RemoveAsync($"turfs_p1_ps100_locall_min0_max0_sortid_asc");
            await cache.RemoveAsync($"turfs_p1_ps10_locall_min0_max0_sort_asc");
            await cache.RemoveAsync($"turfs_p1_ps50_locall_min0_max0_sort_asc");
            await cache.RemoveAsync($"turfs_p1_ps100_locall_min0_max0_sort_asc");
        }
        catch { }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Settings updated successfully"));
    }

    [HttpPost("clear-cache")]
    public async Task<IActionResult> ClearCache([FromServices] IDistributedCache cache)
    {
        await cache.RemoveAsync($"turfs_p1_ps10_locall_min0_max0_sortid_asc");
        await cache.RemoveAsync($"turfs_p1_ps50_locall_min0_max0_sortid_asc");
        await cache.RemoveAsync($"turfs_p1_ps100_locall_min0_max0_sortid_asc");
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Cache cleared"));
    }
}

public class TurfSettingsDto 
{
    public string? TurfName { get; set; }
    public decimal? PricePerHour { get; set; }
    public decimal? DayTimePrice { get; set; }
    public decimal? AfternoonPrice { get; set; }
    public decimal? NightTimePrice { get; set; }
    public bool? IsActive { get; set; }
}
