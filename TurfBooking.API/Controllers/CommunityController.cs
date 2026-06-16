using Application.Common.Result;
using Application.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;

namespace TurfBooking.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class CommunityController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public CommunityController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        var bookings = await _unitOfWork.Bookings.AsQueryable()
            .Include(b => b.Slot)
            .ThenInclude(s => s.Turf)
            .ToListAsync();

        var leaderboard = users
            .Where(u => u.Role != "SuperAdmin")
            .Select(u => 
            {
                var userBookings = bookings.Where(b => b.UserId == u.Id).ToList();
                var latestBooking = userBookings.OrderByDescending(b => b.CreatedAt).FirstOrDefault();
                
                return new
                {
                    Id = u.Id.ToString(),
                    Name = u.Name,
                    Avatar = string.IsNullOrEmpty(u.ProfilePictureUrl) ? $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(u.Name)}&background=random" : u.ProfilePictureUrl,
                    Points = userBookings.Count,
                    TurfName = latestBooking?.Slot?.Turf?.TurfName ?? "Various Turfs"
                };
            })
            .Where(u => u.Points > 0)
            .OrderByDescending(x => x.Points)
            .ToList();

        // Assign ranks and trends
        var finalLeaderboard = leaderboard.Select((x, index) => new
        {
            x.Id,
            x.Name,
            x.Avatar,
            x.Points,
            x.TurfName,
            Rank = index + 1,
            Trend = index % 3 == 0 ? "up" : index % 3 == 1 ? "same" : "down" // Mock trend for now
        }).ToList();

        return Ok(ApiResponse<object>.SuccessResponse(finalLeaderboard, "Leaderboard retrieved successfully"));
    }
}
