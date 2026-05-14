
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]

[Route("api/[controller]")]

public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(
        IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]

    public async Task<IActionResult> Register(
        RegisterRequestDto request)
    {
        var result =
            await _authService.RegisterAsync(request);

        return Ok(result);
    }

    [HttpPost("login")]

    public async Task<IActionResult> Login(
        LoginRequestDto request)
    {
        var result =
            await _authService.LoginAsync(request);

        if (result == null)
        {
            return Unauthorized(
                AuthMessages.InvalidCredentials);
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