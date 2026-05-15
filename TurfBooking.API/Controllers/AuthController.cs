using Application.Common.Messages;
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
                return BadRequest(new
                {
                    message = result.Error ?? "Registration failed"
                });
            }

            return Ok(result);
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
                return Unauthorized(new
                {
                    message = result.Error ?? AuthMessages.InvalidCredentials
                });
            }

            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(
            string refreshToken)
        {
            var result =
                await _authService
                    .RefreshTokenAsync(refreshToken);

            if (result == null)
            {
                return Unauthorized();
            }

            return Ok(result);
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting("ForgotPasswordPolicy")]
        public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequestDto request)
        {
            var result =
                await _authService
                    .ForgotPasswordAsync(request);

            return Ok(new
            {
                Message = AuthMessages.ResetLinkSent,
                Token = result
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
        ResetPasswordRequestDto request)
        {
            var result =
                await _authService
                    .ResetPasswordAsync(request);

            return Ok(result);
        }
    }
}