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
[Authorize]
public class OwnerRequestController : ControllerBase
{
    private readonly IMediator _mediator;

    public OwnerRequestController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> SubmitRequest([FromBody] CreateOwnerRequestDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));

        var userId = int.Parse(userIdClaim);

        var result = await _mediator.Send(new CreateOwnerRequestCommand(userId, dto.TurfId, dto.BusinessName, dto.ContactNumber, dto.ProofDocumentUrl));
        
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(result.Error ?? "Submission failed", null, 400));
        }

        return Ok(ApiResponse<int>.SuccessResponse(result.Value, "Owner request submitted successfully"));
    }
}

public class CreateOwnerRequestDto
{
    public int TurfId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string ProofDocumentUrl { get; set; } = string.Empty;
}
