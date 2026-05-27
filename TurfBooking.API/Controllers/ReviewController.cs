using Application.Common.Result;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TurfBooking.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(CreateReviewDto dto, CancellationToken ct = default)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", null, 401));

            var userId = int.Parse(userIdClaim);
            var result = await _reviewService.CreateReviewAsync(dto, userId, ct);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(result.Error, null, 400));
            }

            return Ok(ApiResponse<ReviewResponseDto>.SuccessResponse(result.Value!, "Review submitted successfully"));
        }

        [HttpGet("turf/{turfId}")]
        public async Task<IActionResult> GetByTurf(int turfId, CancellationToken ct = default)
        {
            var result = await _reviewService.GetReviewsByTurfAsync(turfId, ct);
            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(result.Error, null, 400));
            }

            return Ok(ApiResponse<IEnumerable<ReviewResponseDto>>.SuccessResponse(result.Value!, "Reviews retrieved successfully"));
        }
    }
}
