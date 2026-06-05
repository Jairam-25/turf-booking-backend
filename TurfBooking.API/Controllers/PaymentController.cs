using Application.Common.Result;
using Application.DTOs;
using Application.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TurfBooking.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(ApiResponse<object>.FailureResponse("Invalid amount", null, 400));
        }

        string receiptId = "receipt_" + Guid.NewGuid().ToString().Substring(0, 8);
        var result = await _paymentService.CreateOrderAsync(request.Amount, receiptId);

        if (!result.IsSuccess)
        {
            return StatusCode(500, ApiResponse<object>.FailureResponse(result.Error, null, 500));
        }

        return Ok(ApiResponse<CreateOrderResponseDto>.SuccessResponse(result.Value, "Order created successfully"));
    }
}
