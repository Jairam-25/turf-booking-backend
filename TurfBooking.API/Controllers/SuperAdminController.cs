using Application.Common.Result;
using Application.Features.OwnerRequests.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using System.Security.Claims;

namespace TurfBooking.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public SuperAdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("owner-requests")]
    public async Task<IActionResult> GetOwnerRequests([FromServices] Application.Interfaces.IUnitOfWork unitOfWork)
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
}

public class ApproveOwnerRequestDto
{
    public int RequestId { get; set; }
}
