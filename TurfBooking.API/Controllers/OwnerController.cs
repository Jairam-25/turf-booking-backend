using Application.Common.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;
using Hangfire;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace TurfBooking.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Owner,SuperAdmin")]
public class OwnerController : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromServices] IUnitOfWork unitOfWork, [FromQuery] int? turfId = null)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));

        var userId = int.Parse(userIdClaim);

        var owner = (await unitOfWork.Owners.GetAllAsync()).FirstOrDefault(o => o.UserId == userId);
        if (owner == null)
        {
            return Ok(ApiResponse<object>.SuccessResponse(new { TurfName = "Unassigned Turf", Stats = new { Revenue = 0, Bookings = 0, Utilization = 0, Pending = 0 }, RecentBookings = new object[0] }, "No owner profile found"));
        }

        var allTurfs = await unitOfWork.Turfs.GetAllAsync();
        var ownerTurfs = allTurfs.Where(t => t.OwnerId == owner.Id).ToList();
        var myTurf = turfId.HasValue ? ownerTurfs.FirstOrDefault(t => t.Id == turfId.Value) : ownerTurfs.FirstOrDefault();
        
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
            if (b.UserId == owner.UserId)
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

        var turfImages = await unitOfWork.TurfImages.GetAllAsync();
        var myImageUrl = turfImages.OrderByDescending(i => i.UploadedAt).FirstOrDefault(i => i.TurfId == myTurf.Id && !i.IsDeleted)?.ImageUrl;

        var dashboardData = new
        {
            TurfId = myTurf.Id,
            TurfName = myTurf.Name,
            PricePerHour = myTurf.PricePerHour,
            DayTimePrice = myTurf.DayTimePrice,
            AfternoonPrice = myTurf.AfternoonPrice,
            NightTimePrice = myTurf.NightTimePrice,
            ImageUrl = myImageUrl,
            VerificationStatus = myTurf.VerificationStatus,
            RemainingDays = Math.Max(0, 365 - (DateTime.UtcNow - owner.CreatedAt).Days),
            Stats = new
            {
                Revenue = recentBookingsDto.Sum(b => b.Amount),
                Bookings = recentBookingsDto.Length,
                Utilization = mySlots.Count > 0 ? (myBookings.Count * 100 / mySlots.Count) : 0,
                Pending = 0
            },
            Analytics = new
            {
                TotalCustomers = recentBookingsDto.Select(b => b.User).Where(u => u != "Offline Booking").Distinct().Count(),
                MonthlyRevenue = recentBookingsDto.Sum(b => b.Amount),
                MonthlyCosts = Math.Round(recentBookingsDto.Sum(b => b.Amount) * 0.25m), // Estimated 25% overhead
                PendingBookings = 0,
                DueToday = mySlots.Count(s => s.StartTime.Date == DateTime.Today && !myBookings.Any(b => b.SlotId == s.Id)),
                Unassigned = 0
            },
            RecentBookings = recentBookingsDto,
            OwnedTurfs = ownerTurfs.Select(t => new { Id = t.Id, Name = t.Name, Status = t.VerificationStatus }).ToList()
        };

        return Ok(ApiResponse<object>.SuccessResponse(dashboardData, "Owner dashboard retrieved successfully"));
    }

    [HttpPost("settings")]
    public async Task<IActionResult> SaveSettings([FromBody] TurfSettingsDto dto, [FromServices] IUnitOfWork unitOfWork, [FromServices] IDistributedCache cache)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));

        var userId = int.Parse(userIdClaim);

        var owner = (await unitOfWork.Owners.GetAllAsync()).FirstOrDefault(o => o.UserId == userId);
        if (owner == null)
            return BadRequest(ApiResponse<object>.FailureResponse("No owner profile found."));

        var allTurfs = await unitOfWork.Turfs.GetAllAsync();
        var myTurf = allTurfs.FirstOrDefault(t => t.OwnerId == owner.Id);

        if (myTurf == null)
            return BadRequest(ApiResponse<object>.FailureResponse("No turf assigned to this owner."));

        myTurf.Name = dto.TurfName ?? myTurf.Name;
        if (dto.PricePerHour.HasValue) myTurf.PricePerHour = dto.PricePerHour.Value;
        
        myTurf.DayTimePrice = dto.DayTimePrice;
        myTurf.AfternoonPrice = dto.AfternoonPrice;
        myTurf.NightTimePrice = dto.NightTimePrice;

        if (!string.IsNullOrEmpty(dto.ImageUrl))
        {
            var existingImages = await unitOfWork.TurfImages.GetAllAsync();
            var myImages = existingImages.Where(i => i.TurfId == myTurf.Id).ToList();
            
            foreach (var img in myImages)
            {
                img.IsDeleted = true;
                img.DeletedAt = DateTime.UtcNow;
            }

            if (dto.ImageUrl != "CLEAR")
            {
                var newImage = new TurfImage 
                { 
                    TurfId = myTurf.Id, 
                    ImageUrl = dto.ImageUrl, 
                    UploadedAt = DateTime.UtcNow 
                };
                await unitOfWork.TurfImages.AddAsync(newImage);
            }
        }

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

    [HttpPost("send-logout-otp")]
    public async Task<IActionResult> SendLogoutOtp(
        [FromServices] IUnitOfWork unitOfWork,
        [FromServices] IDistributedCache cache,
        [FromServices] IBackgroundJobClient backgroundJobs)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));

        var userId = int.Parse(userIdClaim);

        // Get the base user to ensure we always have an email address
        var baseUser = await unitOfWork.Users.GetByIdAsync(userId);
        if (baseUser == null)
            return BadRequest(ApiResponse<object>.FailureResponse("User profile not found"));

        var otpCode = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        // Save OTP to cache for 5 minutes
        var cacheKey = $"logout_otp:{userId}";
        await cache.SetStringAsync(cacheKey, otpCode, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        // Always log OTP to console for dev visibility (master code 123456 also works in dev)
        Console.WriteLine($"[INFO] Logout OTP for user {userId}: {otpCode}");

        // Fire-and-forget email via Hangfire (same pattern as OtpService)
        // This never blocks the response — Hangfire will retry on SMTP failure
        if (!string.IsNullOrEmpty(baseUser.Email))
        {
            backgroundJobs.Enqueue<IEmailService>(
                svc => svc.SendOtpEmailAsync(baseUser.Email, otpCode));
        }

        // Return masked email
        var maskedEmail = "your registered email";
        if (!string.IsNullOrEmpty(baseUser.Email) && baseUser.Email.Contains('@'))
        {
            var parts = baseUser.Email.Split('@');
            maskedEmail = parts[0].Length > 1
                ? $"{parts[0][0]}***@{parts[1]}"
                : baseUser.Email;
        }

        return Ok(ApiResponse<object>.SuccessResponse(new { email = maskedEmail }, "Logout verification OTP sent successfully."));
    }

    [HttpPost("verify-logout-otp")]
    public async Task<IActionResult> VerifyLogoutOtp(
        [FromBody] VerifyLogoutOtpDto dto,
        [FromServices] IDistributedCache cache,
        [FromServices] IUnitOfWork unitOfWork,
        [FromServices] IUserRepository userRepository)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));

        var userId = int.Parse(userIdClaim);
        var cacheKey = $"logout_otp:{userId}";

        var storedOtp = await cache.GetStringAsync(cacheKey);
        
        // Dev master code backdoor for easy testing
        bool isDevMasterCode = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" && dto.OtpCode == "123456";

        if (!isDevMasterCode)
        {
            if (string.IsNullOrEmpty(storedOtp))
                return BadRequest(ApiResponse<object>.FailureResponse("OTP has expired or is invalid."));

            if (storedOtp != dto.OtpCode)
                return BadRequest(ApiResponse<object>.FailureResponse("Invalid OTP code."));
        }

        // Clear OTP from cache
        await cache.RemoveAsync(cacheKey);

        // Terminate Partnership logic: remove owner, turfs, and change role to User
        var user = await userRepository.GetByIdAsync(userId);
        if (user != null)
        {
            user.Role = "User"; // Revert to regular user
        }

        var owners = await unitOfWork.Owners.GetAllAsync();
        var owner = owners.FirstOrDefault(o => o.UserId == userId);
        
        if (owner != null)
        {
            owner.IsDeleted = true;
            owner.DeletedAt = DateTime.UtcNow;

            var turfs = await unitOfWork.Turfs.GetAllAsync();
            var ownerTurfs = turfs.Where(t => t.OwnerId == owner.Id).ToList();
            
            foreach (var turf in ownerTurfs)
            {
                turf.IsDeleted = true;
                turf.DeletedAt = DateTime.UtcNow;
                
                var docs = await unitOfWork.TurfDocuments.GetAllAsync();
                foreach (var doc in docs.Where(d => d.TurfId == turf.Id))
                {
                    doc.IsDeleted = true;
                    doc.DeletedAt = DateTime.UtcNow;
                }

                var imgs = await unitOfWork.TurfImages.GetAllAsync();
                foreach (var img in imgs.Where(i => i.TurfId == turf.Id))
                {
                    img.IsDeleted = true;
                    img.DeletedAt = DateTime.UtcNow;
                }
            }
        }

        await unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Partnership terminated. You are now a regular user."));
    }

    [HttpDelete("booking/{id}")]
    public async Task<IActionResult> CancelBooking(
        int id,
        [FromQuery] string reason,
        [FromServices] IUnitOfWork unitOfWork,
        [FromServices] IDistributedCache cache,
        [FromServices] IServiceScopeFactory scopeFactory)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));

        var userId = int.Parse(userIdClaim);
        var owner = (await unitOfWork.Owners.GetAllAsync()).FirstOrDefault(o => o.UserId == userId);
        if (owner == null)
            return BadRequest(ApiResponse<object>.FailureResponse("No owner profile found."));

        var booking = await unitOfWork.Bookings.AsQueryable()
            .Include(b => b.Slot)
            .ThenInclude(s => s!.Turf)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
            return NotFound(ApiResponse<object>.FailureResponse("Booking not found."));

        // Ensure the owner actually owns the turf where the booking was made
        if (booking.Slot?.Turf?.OwnerId != owner.Id)
            return Forbid();

        // Free the slot
        if (booking.Slot != null)
            booking.Slot.IsBooked = false;

        await unitOfWork.Bookings.DeleteAsync(booking.Id);
        await unitOfWork.SaveChangesAsync();

        if (booking.Slot != null)
        {
            try
            {
                await cache.RemoveAsync($"slots_turf_{booking.Slot.TurfId}");
            }
            catch { }
        }

        // Notify user via email if applicable
        if (booking.User != null && !string.IsNullOrEmpty(booking.User.Email) && booking.Slot != null && booking.Slot.Turf != null)
        {
            var email = booking.User.Email;
            var name = booking.User.Name;
            var turfName = booking.Slot.Turf.Name;
            var date = booking.BookingDate;

            _ = Task.Run(async () =>
            {
                using var scope = scopeFactory.CreateScope();
                var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                try
                {
                    await emailSvc.SendBookingCancellationEmailAsync(email, name, turfName, date, $"Cancelled by Owner: {reason}");
                }
                catch { }
            });
        }

        return Ok(ApiResponse<string>.SuccessResponse(string.Empty, "Booking cancelled successfully."));
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
    public string? ImageUrl { get; set; }
}

public class VerifyLogoutOtpDto
{
    public string OtpCode { get; set; } = string.Empty;
}
