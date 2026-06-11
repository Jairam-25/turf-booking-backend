using Application.Common.Messages;
using Application.Common.Result;
using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Asp.Versioning;

using Application.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace TurfBooking.API.Controllers
{

    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController(IOtpService otpService, IAuthService authService) : ControllerBase
    {
        private readonly IOtpService _otpService = otpService;
        private readonly IAuthService _authService = authService;

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp(SendOtpRequestDto request)
        {
            var result = await _otpService.SendOtpAsync(request);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(result.Error ?? "Failed to send OTP", null, 400));
            }

            return Ok(ApiResponse<string>.SuccessResponse(result.Value ?? string.Empty, "OTP sent successfully"));
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpRequestDto request)
        {
            var result = await _otpService.VerifyOtpAsync(request);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(result.Error ?? "Verification failed", null, 400));
            }

            return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result.Value, "Verification successful"));
        }

        [HttpPost("register")]
        [EnableRateLimiting("RegisterPolicy")]
        public async Task<IActionResult> Register(
            RegisterRequestDto request)
        {
            var result = await _authService.RegisterAsync(request);

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
            var result = await _authService.LoginAsync(request);

            if (!result.IsSuccess)
            {
                return Unauthorized(ApiResponse<object>.FailureResponse(result.Error ?? AuthMessages.InvalidCredentials, null, 401));
            }

            return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result.Value, "Login successful"));
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(
            [FromBody] string refreshToken)
        {
            var result = await _authService.RefreshTokenAsync(refreshToken);

            if (!result.IsSuccess)
            {
                return Unauthorized(ApiResponse<object>.FailureResponse(result.Error ?? "Invalid refresh token", null, 401));
            }

            return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result.Value, "Token refreshed"));
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleSignIn(
            [FromBody] GoogleSignInRequestDto request,
            [FromServices] IAuthService authService)
        {
            var result = await authService.GoogleSignInAsync(request);

            if (!result.IsSuccess)
            {
                return Unauthorized(ApiResponse<object>.FailureResponse(result.Error ?? "Google login failed", null, 401));
            }

            return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result.Value, "Google login successful"));
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromServices] IAuthService authService)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            var result = await authService.LogoutAsync(userId);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(result.Error ?? "Logout failed", null, 400));
            }

            return Ok(ApiResponse<string>.SuccessResponse(result.Value ?? string.Empty, "Logged out successfully"));
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting("ForgotPasswordPolicy")]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordRequestDto request)
        {
            var result = await _authService.ForgotPasswordAsync(request);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(result.Error ?? "Forgot password request failed", null, 400));
            }

            return Ok(ApiResponse<string>.SuccessResponse(result.Value ?? string.Empty, AuthMessages.ResetLinkSent));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordRequestDto request)
        {
            var result = await _authService.ResetPasswordAsync(request);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(result.Error ?? "Password reset failed", null, 400));
            }

            return Ok(ApiResponse<string>.SuccessResponse(result.Value ?? string.Empty, "Password reset successfully"));
        }

        [HttpPost("update-fcm-token")]
        [Authorize]
        public async Task<IActionResult> UpdateFcmToken(
            [FromBody] UpdateFcmTokenDto request,
            [FromServices] IUserRepository userRepository,
            [FromServices] IUnitOfWork unitOfWork)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await userRepository.GetByIdAsync(int.Parse(userId));
            if (user == null) return NotFound();

            user.FcmToken = request.Token;
            await unitOfWork.SaveChangesAsync();
            return Ok(ApiResponse<string>.SuccessResponse(string.Empty, "FCM Token updated"));
        }

        [HttpPut("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] UpdateProfileDto request,
            [FromServices] IUserRepository userRepository,
            [FromServices] IUnitOfWork unitOfWork)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await userRepository.GetByIdAsync(int.Parse(userId));
            if (user == null) return NotFound();

            user.Name = request.Name;
            user.PhoneNumber = request.PhoneNumber;
            user.Address = request.Address;
            user.State = request.State;
            user.MaritalStatus = request.MaritalStatus;
            user.PlayerType = request.PlayerType;
            user.PlayingLevel = request.PlayingLevel;
            
            if (!string.IsNullOrEmpty(request.ProfilePictureUrl))
            {
                user.ProfilePictureUrl = request.ProfilePictureUrl;
            }
            
            await unitOfWork.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResponse(new { 
                user.Name, 
                user.Email, 
                user.PhoneNumber, 
                user.ProfilePictureUrl,
                user.Address,
                user.State,
                user.MaritalStatus,
                user.PlayerType,
                user.PlayingLevel
            }, "Profile updated successfully"));
        }

        [HttpDelete("delete-account")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount(
            [FromServices] IUserRepository userRepository,
            [FromServices] IUnitOfWork unitOfWork)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await userRepository.GetByIdAsync(int.Parse(userId));
            if (user == null) return NotFound();

            await userRepository.DeleteAsync(user);
            await unitOfWork.SaveChangesAsync();
            return Ok(ApiResponse<string>.SuccessResponse(string.Empty, "Account deleted successfully"));
        }
    }
}