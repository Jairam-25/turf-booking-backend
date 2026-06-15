using Application.Common.Result;
using Application.Features.OwnerRequests.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using System.Security.Claims;
using Application.Interfaces;
using Domain.Entities;

namespace TurfBooking.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFcmNotificationService _fcmService;

    public SuperAdminController(IMediator mediator, IFcmNotificationService fcmService)
    {
        _mediator = mediator;
        _fcmService = fcmService;
    }

    [HttpGet("dashboard-metrics")]
    public async Task<IActionResult> GetDashboardMetrics([FromServices] IUnitOfWork unitOfWork)
    {
        var users = await unitOfWork.Users.GetAllAsync();
        var turfs = await unitOfWork.Turfs.GetAllAsync();
        var bookings = await unitOfWork.Bookings.GetAllAsync();

        int totalUsers = users.Count();
        int totalOwners = users.Count(u => u.Role == "Owner");
        int totalTurfs = turfs.Count();
        int totalBookings = bookings.Count();

        // Rough revenue estimation for the super admin dashboard
        decimal avgPrice = turfs.Any() ? turfs.Average(t => t.PricePerHour) : 500m;
        decimal totalRevenue = totalBookings * avgPrice;

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            users = totalUsers,
            owners = totalOwners,
            turfs = totalTurfs,
            bookings = totalBookings,
            revenue = Math.Round(totalRevenue, 2)
        }, "Metrics retrieved"));
    }

    [HttpGet("owner-requests")]
    public async Task<IActionResult> GetOwnerRequests([FromServices] IUnitOfWork unitOfWork)
    {
        var requests = await unitOfWork.OwnerRequests.GetAllAsync();

        var dtoList = requests.Select(r => new
        {
            id = r.Id,
            user = $"User ID: {r.UserId}",
            turf = $"Turf ID: {r.TurfId}",
            business = r.BusinessName,
            status = r.Status,
            date = r.RequestedAt.ToString("yyyy-MM-dd")
        }).ToList();

        return Ok(ApiResponse<object>.SuccessResponse(dtoList, "Requests retrieved"));
    }

    [HttpPost("approve-owner")]
    public async Task<IActionResult> ApproveOwnerRequest([FromBody] ApproveOwnerRequestDto dto)
    {
        var superAdminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (superAdminIdClaim == null) return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));

        var superAdminId = int.Parse(superAdminIdClaim);

        var result = await _mediator.Send(new ApproveOwnerRequestCommand(dto.RequestId, superAdminId));

        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(result.Error ?? "Approval failed", null, 400));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Owner request approved successfully"));
    }

    [HttpGet("verifications")]
    public async Task<IActionResult> GetVerifications([FromServices] IUnitOfWork unitOfWork)
    {
        var owners = await unitOfWork.Owners.GetAllAsync();
        var turfs = await unitOfWork.Turfs.GetAllAsync();
        var users = await unitOfWork.Users.GetAllAsync();
        var documents = await unitOfWork.TurfDocuments.GetAllAsync();
        var images = await unitOfWork.TurfImages.GetAllAsync();


        var verificationsList = new List<object>();

        foreach (var o in owners)
        {
            var user = users.FirstOrDefault(u => u.Id == o.UserId);
            var ownerTurfs = turfs.Where(t => t.OwnerId == o.Id).ToList();

            if (!ownerTurfs.Any())
            {
                verificationsList.Add(new
                {
                    ownerId = o.Id,
                    userId = o.UserId,
                    fullName = o.FullName,
                    mobileNumber = o.MobileNumber,
                    email = o.Email,
                    address = o.Address,
                    status = o.VerificationStatus,
                    rejectionReason = o.RejectionReason,
                    createdAt = o.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    turfs = new List<object>()
                });
            }
            else
            {
                var turfList = new List<object>();
                foreach (var turf in ownerTurfs)
                {
                    var turfDocs = documents.Where(d => d.TurfId == turf.Id).Select(d => new { d.DocumentType, d.FileUrl }).ToList();
                    var turfImgs = images.Where(i => i.TurfId == turf.Id).Select(i => i.ImageUrl).ToList();

                    turfList.Add(new
                    {
                        turfId = turf.Id,
                        turfName = turf.Name,
                        description = turf.Description,
                        turfType = turf.TurfType,
                        address = turf.Address,
                        city = turf.City,
                        state = turf.State,
                        pincode = turf.Pincode,
                        googleMapLocation = turf.Location,
                        contactNumber = turf.ContactNumber,
                        verificationStatus = turf.VerificationStatus,
                        documents = turfDocs,
                        images = turfImgs
                    });
                }

                verificationsList.Add(new
                {
                    ownerId = o.Id,
                    userId = o.UserId,
                    fullName = o.FullName,
                    mobileNumber = o.MobileNumber,
                    email = o.Email,
                    address = o.Address,
                    status = o.VerificationStatus, // General owner status
                    rejectionReason = o.RejectionReason,
                    createdAt = o.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    turfs = turfList
                });
            }
        }

        return Ok(ApiResponse<object>.SuccessResponse(verificationsList, "Verifications list retrieved successfully."));
    }

    [HttpPost("verify")]
    public async Task<IActionResult> VerifyOwner([FromBody] VerifyOwnerDto dto, [FromServices] IUnitOfWork unitOfWork, [FromServices] IUserRepository userRepository)
    {
        var superAdminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (superAdminIdClaim == null) return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));
        var superAdminId = int.Parse(superAdminIdClaim);

        var owners = await unitOfWork.Owners.GetAllAsync();
        var owner = owners.FirstOrDefault(o => o.Id == dto.OwnerId);
        if (owner == null) return BadRequest(ApiResponse<object>.FailureResponse("Owner profile not found."));

        var user = await userRepository.GetByIdAsync(owner.UserId);
        if (user == null) return BadRequest(ApiResponse<object>.FailureResponse("User not found."));

        var turfs = await unitOfWork.Turfs.GetAllAsync();
        var turf = dto.TurfId.HasValue ? turfs.FirstOrDefault(t => t.Id == dto.TurfId.Value) : turfs.FirstOrDefault(t => t.OwnerId == owner.Id);

        owner.VerificationStatus = dto.Status;
        if (dto.Status == "Rejected")
        {
            owner.RejectionReason = dto.RejectionReason;
        }
        else
        {
            owner.RejectionReason = null;
        }

        if (turf != null)
        {
            turf.VerificationStatus = dto.Status;
        }

        if (dto.Status == "Approved")
        {
            // Activate Owner Account role
            user.Role = "Owner";
        }
        else if (dto.Status == "Rejected")
        {
            // Revert role to User if approved previously, or keep as User
            user.Role = "User";
        }

        // Create Audit Log
        var auditLog = new AuditLog
        {
            UserId = superAdminId,
            Action = $"OwnerVerification_{dto.Status}",
            Details = $"SuperAdmin {superAdminId} set verification status to {dto.Status} for Owner {owner.Id} (User {user.Id}). Reason: {dto.RejectionReason}"
        };
        await unitOfWork.AuditLogs.AddAsync(auditLog);

        await unitOfWork.SaveChangesAsync();

        // Send Push Notification if FCM token is available
        if (!string.IsNullOrEmpty(user.FcmToken))
        {
            var title = dto.Status == "Approved" ? "🎉 Owner Account Approved!" : "⚠️ Verification Update";
            var body = dto.Status == "Approved"
                ? "Your owner profile has been approved. You can now access your Owner Dashboard!"
                : $"Your owner verification status has been updated to '{dto.Status}'." + (dto.Status == "Rejected" ? $" Reason: {dto.RejectionReason}" : "");

            try
            {
                await _fcmService.SendPushNotificationAsync(user.FcmToken, title, body);
            }
            catch { }
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, $"Verification status set to {dto.Status} successfully."));
    }

    [HttpPost("remove-owner")]
    public async Task<IActionResult> RemoveOwner([FromBody] RemoveOwnerDto dto, [FromServices] IUnitOfWork unitOfWork, [FromServices] IUserRepository userRepository)
    {
        var superAdminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (superAdminIdClaim == null) return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));
        var superAdminId = int.Parse(superAdminIdClaim);

        var owners = await unitOfWork.Owners.GetAllAsync();
        var owner = owners.FirstOrDefault(o => o.Id == dto.OwnerId);
        if (owner == null) return BadRequest(ApiResponse<object>.FailureResponse("Owner profile not found."));

        var user = await userRepository.GetByIdAsync(owner.UserId);
        if (user != null)
        {
            user.Role = "User";
        }

        // Delete the associated turf(s)
        var turfs = await unitOfWork.Turfs.GetAllAsync();
        var ownerTurfs = turfs.Where(t => t.OwnerId == owner.Id).ToList();
        foreach (var turf in ownerTurfs)
        {
            turf.IsDeleted = true;
            turf.DeletedAt = DateTime.UtcNow;

            // Delete associated documents and images
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

        // Soft delete the owner
        owner.IsDeleted = true;
        owner.DeletedAt = DateTime.UtcNow;

        // Also delete owner payments
        var payments = await unitOfWork.OwnerPayments.GetAllAsync();
        var ownerPayments = payments.Where(p => p.OwnerId == owner.Id).ToList();
        foreach (var payment in ownerPayments)
        {
            payment.IsDeleted = true;
            payment.DeletedAt = DateTime.UtcNow;
        }

        // Create Audit Log
        var auditLog = new AuditLog
        {
            UserId = superAdminId,
            Action = "RemoveOwner",
            Details = $"SuperAdmin {superAdminId} removed Owner {owner.Id} (User {owner.UserId}) and all their turfs/documents/payments."
        };
        await unitOfWork.AuditLogs.AddAsync(auditLog);

        await unitOfWork.SaveChangesAsync();

        // Send Push Notification if FCM token is available
        if (user != null && !string.IsNullOrEmpty(user.FcmToken))
        {
            var title = "⚠️ Owner Profile Removed";
            var body = "Your turf owner profile has been removed by the administrator. You will need to re-register to list a turf.";
            try
            {
                await _fcmService.SendPushNotificationAsync(user.FcmToken, title, body);
            }
            catch { }
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Owner and all associated turfs removed successfully."));
    }

    [HttpPost("edit-owner")]
    public async Task<IActionResult> EditOwner([FromBody] EditOwnerDto dto, [FromServices] IUnitOfWork unitOfWork)
    {
        var superAdminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (superAdminIdClaim == null) return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));
        var superAdminId = int.Parse(superAdminIdClaim);

        var owners = await unitOfWork.Owners.GetAllAsync();
        var owner = owners.FirstOrDefault(o => o.Id == dto.OwnerId);
        if (owner == null) return BadRequest(ApiResponse<object>.FailureResponse("Owner profile not found."));

        owner.FullName = dto.FullName ?? owner.FullName;
        owner.MobileNumber = dto.MobileNumber ?? owner.MobileNumber;
        owner.Email = dto.Email ?? owner.Email;
        owner.Address = dto.Address ?? owner.Address;

        // Create Audit Log
        var auditLog = new AuditLog
        {
            UserId = superAdminId,
            Action = "EditOwner",
            Details = $"SuperAdmin {superAdminId} edited details of Owner {owner.Id}."
        };
        await unitOfWork.AuditLogs.AddAsync(auditLog);

        await unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Owner details updated successfully."));
    }
}

public class ApproveOwnerRequestDto
{
    public int RequestId { get; set; }
}

public class VerifyOwnerDto
{
    public int OwnerId { get; set; }
    public int? TurfId { get; set; }
    public string Status { get; set; } = string.Empty; // Approved, Rejected, Under Review, Pending Verification
    public string? RejectionReason { get; set; }
}

public class RemoveOwnerDto
{
    public int OwnerId { get; set; }
}

public class EditOwnerDto
{
    public int OwnerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}
