using Application.Common.Messages;
using Application.Common.Result;
using Application.DTOs;
using Application.Features.Auth.Commands;
using MediatR;
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
    public class AuthController(IMediator mediator, IOtpService otpService) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;
        private readonly IOtpService _otpService = otpService;

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
            var result = await _mediator.Send(
                new RegisterCommand(request));

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
            var result = await _mediator.Send(
                new LoginCommand(request));

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
            var result = await _mediator.Send(
                new RefreshTokenCommand(refreshToken));

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
            var result = await _mediator.Send(
                new ForgotPasswordCommand(request));

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
            var result = await _mediator.Send(
                new ResetPasswordCommand(request));

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