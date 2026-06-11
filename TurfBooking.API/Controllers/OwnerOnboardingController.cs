using Application.Common.Result;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using System.Security.Claims;

namespace TurfBooking.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class OwnerOnboardingController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFcmNotificationService _fcmService;

    public OwnerOnboardingController(IUnitOfWork unitOfWork, IFcmNotificationService fcmService)
    {
        _unitOfWork = unitOfWork;
        _fcmService = fcmService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetOnboardingStatus()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));
        var userId = int.Parse(userIdClaim);

        var owners = await _unitOfWork.Owners.GetAllAsync();
        var owner = owners.FirstOrDefault(o => o.UserId == userId);

        if (owner == null)
        {
            return Ok(ApiResponse<object>.SuccessResponse(new { status = "NotRegistered" }, "Not registered as an owner"));
        }

        // Check if there are any successful payments
        var payments = await _unitOfWork.OwnerPayments.GetAllAsync();
        var hasPaid = payments.Any(p => p.OwnerId == owner.Id && p.Status == "Success");

        if (!hasPaid)
        {
            return Ok(ApiResponse<object>.SuccessResponse(new { status = "PendingPayment", ownerId = owner.Id }, "Payment pending"));
        }

        // Check if turf details are submitted
        var turfs = await _unitOfWork.Turfs.GetAllAsync();
        var turf = turfs.FirstOrDefault(t => t.OwnerId == owner.Id);

        if (turf == null)
        {
            return Ok(ApiResponse<object>.SuccessResponse(new { status = "PendingTurfDetails", ownerId = owner.Id }, "Turf details pending"));
        }

        // Return current verification status
        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            status = owner.VerificationStatus, // Pending Verification, Under Review, Approved, Rejected
            ownerId = owner.Id,
            turfId = turf.Id,
            rejectionReason = owner.RejectionReason,
            turfVerificationStatus = turf.VerificationStatus
        }, $"Onboarding status: {owner.VerificationStatus}"));
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterOwner([FromBody] RegisterOwnerDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));
        var userId = int.Parse(userIdClaim);

        var owners = await _unitOfWork.Owners.GetAllAsync();
        var existingOwner = owners.FirstOrDefault(o => o.UserId == userId);

        if (existingOwner != null)
        {
            return BadRequest(ApiResponse<object>.FailureResponse("You have already started the registration process.", null, 400));
        }

        var owner = new Owner
        {
            UserId = userId,
            FullName = dto.FullName,
            MobileNumber = dto.MobileNumber,
            Email = dto.Email,
            Address = dto.Address,
            VerificationStatus = "Pending Verification", // Start state, but will proceed to payment
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Owners.AddAsync(owner);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResponse(new { ownerId = owner.Id }, "Owner information saved. Proceed to payment."));
    }

    [HttpPost("payment")]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentDto dto)
    {
        var owners = await _unitOfWork.Owners.GetAllAsync();
        var owner = owners.FirstOrDefault(o => o.Id == dto.OwnerId);
        if (owner == null) return BadRequest(ApiResponse<object>.FailureResponse("Owner profile not found."));

        var payment = new OwnerPayment
        {
            OwnerId = dto.OwnerId,
            RazorpayPaymentId = dto.RazorpayPaymentId,
            Amount = dto.Amount,
            Status = dto.Status, // Success or Failed
            PaymentDate = DateTime.UtcNow
        };

        await _unitOfWork.OwnerPayments.AddAsync(payment);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResponse(new { paymentId = payment.Id, status = payment.Status }, "Payment processed successfully."));
    }

    [HttpPost("submit-turf")]
    public async Task<IActionResult> SubmitTurfDetails([FromBody] SubmitTurfDto dto)
    {
        var owners = await _unitOfWork.Owners.GetAllAsync();
        var owner = owners.FirstOrDefault(o => o.Id == dto.OwnerId);
        if (owner == null) return BadRequest(ApiResponse<object>.FailureResponse("Owner profile not found."));

        // If resubmitting, we can update the previous turf unless explicitly creating a new one
        var existingTurfs = await _unitOfWork.Turfs.GetAllAsync();
        var existingTurf = existingTurfs.FirstOrDefault(t => t.OwnerId == owner.Id);

        bool isDuplicate = existingTurfs.Any(t =>
            string.Equals(t.Name, dto.TurfName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(t.Location, dto.GoogleMapLocation, StringComparison.OrdinalIgnoreCase) &&
            (dto.IsNewTurf || existingTurf == null || t.Id != existingTurf.Id));

        if (isDuplicate)
            return BadRequest(ApiResponse<object>.FailureResponse("A turf with this name and location already exists."));

        Turf turf;
        if (!dto.IsNewTurf && existingTurf != null)
        {
            // Update existing turf for resubmission
            turf = existingTurf;
            turf.Name = dto.TurfName;
            turf.Description = dto.Description;
            turf.TurfType = dto.TurfType;
            turf.Address = dto.Address;
            turf.City = dto.City;
            turf.State = dto.State;
            turf.Pincode = dto.Pincode;
            turf.Location = dto.GoogleMapLocation;
            turf.ContactNumber = dto.ContactNumber;
            turf.VerificationStatus = "Pending Verification";

            // Remove previous images and documents to avoid duplicates on resubmission
            var allDocs = await _unitOfWork.TurfDocuments.GetAllAsync();
            var prevDocs = allDocs.Where(d => d.TurfId == turf.Id).ToList();
            foreach (var doc in prevDocs) doc.IsDeleted = true;

            var allImgs = await _unitOfWork.TurfImages.GetAllAsync();
            var prevImgs = allImgs.Where(i => i.TurfId == turf.Id).ToList();
            foreach (var img in prevImgs) img.IsDeleted = true;
        }
        else
        {
            // Create new turf
            turf = new Turf
            {
                OwnerId = owner.Id,
                Name = dto.TurfName,
                Description = dto.Description,
                TurfType = dto.TurfType,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                Pincode = dto.Pincode,
                Location = dto.GoogleMapLocation,
                ContactNumber = dto.ContactNumber,
                VerificationStatus = "Pending Verification",
                PricePerHour = 1000m // Default price per hour
            };
            await _unitOfWork.Turfs.AddAsync(turf);
            await _unitOfWork.SaveChangesAsync(); // save to generate Turf.Id
        }

        // Add new images
        foreach (var imgUrl in dto.Images)
        {
            var turfImg = new TurfImage
            {
                TurfId = turf.Id,
                ImageUrl = imgUrl,
                UploadedAt = DateTime.UtcNow
            };
            await _unitOfWork.TurfImages.AddAsync(turfImg);
        }

        // Add new documents
        foreach (var docDto in dto.Documents)
        {
            var turfDoc = new TurfDocument
            {
                TurfId = turf.Id,
                DocumentType = docDto.DocumentType,
                FileUrl = docDto.FileUrl,
                UploadedAt = DateTime.UtcNow
            };
            await _unitOfWork.TurfDocuments.AddAsync(turfDoc);
        }

        // Update owner verification status
        owner.VerificationStatus = "Pending Verification";
        owner.RejectionReason = null; // Clear previous rejection reason if resubmitting

        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResponse(new { turfId = turf.Id }, "Turf details and documents submitted for verification."));
    }
}

public class RegisterOwnerDto
{
    public string FullName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class ProcessPaymentDto
{
    public int OwnerId { get; set; }
    public string RazorpayPaymentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Success";
}

public class SubmitTurfDto
{
    public int OwnerId { get; set; }
    public bool IsNewTurf { get; set; }
    public string TurfName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TurfType { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string GoogleMapLocation { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public List<string> Images { get; set; } = new();
    public List<SubmitDocumentDto> Documents { get; set; } = new();
}

public class SubmitDocumentDto
{
    public string DocumentType { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
}
