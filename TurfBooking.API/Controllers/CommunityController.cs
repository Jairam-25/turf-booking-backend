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
        var bookings = await _unitOfWork.Bookings.GetAllAsync();

        var leaderboard = users
            .Where(u => u.Role != "SuperAdmin")
            .Select(u => new
            {
                Id = u.Id.ToString(),
                Name = u.Name,
                Avatar = string.IsNullOrEmpty(u.ProfilePictureUrl) ? $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(u.Name)}&background=random" : u.ProfilePictureUrl,
                Points = bookings.Count(b => b.UserId == u.Id),
            })
            .Where(u => u.Points > 0)
            .OrderByDescending(x => x.Points)
            .Take(20)
            .ToList();

        // Assign ranks and trends
        var finalLeaderboard = leaderboard.Select((x, index) => new
        {
            x.Id,
            x.Name,
            x.Avatar,
            x.Points,
            Rank = index + 1,
            Trend = index % 3 == 0 ? "up" : index % 3 == 1 ? "same" : "down" // Mock trend for now
        }).ToList();

        return Ok(ApiResponse<object>.SuccessResponse(finalLeaderboard, "Leaderboard retrieved successfully"));
    }
}
