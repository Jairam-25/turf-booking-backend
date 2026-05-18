using Application.Common.Messages;
using Application.Common.Result;
using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace TurfBooking.API.Controllers
{

    [ApiController]

    [Route("api/[controller]")]

    public class AuthController(IAuthService authService) : ControllerBase
    {
        private readonly IAuthService _authService = authService;        

        [HttpPost("register")]
        [EnableRateLimiting("RegisterPolicy")]
        public async Task<IActionResult> Register(
            RegisterRequestDto request)
        {
            var result =
                await _authService.RegisterAsync(request);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(result.Error ?? "Registration failed", null, 400));
            }

            return Ok(ApiResponse<string>.SuccessResponse(result.Value, "Registration successful"));
        }

        [HttpPost("login")]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> Login(
            LoginRequestDto request)
        {
            var result =
                await _authService.LoginAsync(request);

            if (!result.IsSuccess)
            {
                return Unauthorized(ApiResponse<object>.FailureResponse(result.Error ?? AuthMessages.InvalidCredentials, null, 401));
            }

            return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result.Value, "Login successful"));
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(
            string refreshToken)
        {
            var result =
                await _authService
                    .RefreshTokenAsync(refreshToken);

            if (!result.IsSuccess)
            {
                return Unauthorized(ApiResponse<object>.FailureResponse(result.Error ?? "Invalid refresh token", null, 401));
            }

            return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result.Value, "Token refreshed"));
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting("ForgotPasswordPolicy")]
        public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequestDto request)
        {
            var result =
                await _authService
                    .ForgotPasswordAsync(request);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(result.Error ?? "Forgot password request failed", null, 400));
            }

            return Ok(ApiResponse<string>.SuccessResponse(result.Value, AuthMessages.ResetLinkSent));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
        ResetPasswordRequestDto request)
        {
            var result =
                await _authService
                    .ResetPasswordAsync(request);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(result.Error ?? "Password reset failed", null, 400));
            }

            return Ok(ApiResponse<string>.SuccessResponse(result.Value, "Password reset successfully"));
        }
    }
}