using Application.Common.Constants;
using Application.Common.Messages;
using Application.Common.Settings;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Persistence.Context;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly IEmailService _emailService;

    public AuthService(
        ApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings,
        IEmailService emailService
    )
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _emailService = emailService;
    }

    public async Task<string> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await _context.Users
        .FirstOrDefaultAsync(x =>
            x.Email == request.Email);

        if (existingUser != null)
        {
            throw new Exception(AuthMessages.EmailAlreadyExists);
        }

        var hashedPassword =
            BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Password = hashedPassword
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        // Sended mail after registeration on Registered mail I'd or mobile number.
        await _emailService.SendWelcomeEmailAsync
        (
            user.Email,
            user.Name
        );

        return AuthMessages.RegisterSuccess;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var user = await _context.Users
        .FirstOrDefaultAsync(x =>
            x.Email == request.EmailOrPhone
            ||
            x.PhoneNumber == request.EmailOrPhone);

        if (user == null)
        {
            return null;
        }

        if (user.IsLocked &&
            user.LockoutEnd > DateTime.UtcNow)
        {
            throw new Exception(
                AuthMessages.LoginMaxAttempt);
        }

        var isPasswordValid =
            BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.Password);

        if (!isPasswordValid)
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= AppConstants.MaxLoginAttempts)
            {
                user.IsLocked = true;

                user.LockoutEnd =
                    DateTime.Now.AddMinutes(AppConstants.LockoutMinutes);
            }

            await _context.SaveChangesAsync();

            return null;
        }

        user.FailedLoginAttempts = 0;
        user.IsLocked = false;

        var token = GenerateJwtToken(user);

        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;

        user.RefreshTokenExpiryTime =
            DateTime.UtcNow.AddDays(AppConstants.RefreshTokenExpiryDays);

        await _context.SaveChangesAsync();

        return new LoginResponseDto
        {
            Name = user.Name,
            Email = user.Email,
            Number = user.PhoneNumber,
            Role = user.Role,
            Token = token,
            RefreshToken = refreshToken
        };
    }

    // Forget password
    public async Task<string> ForgotPasswordAsync(
    ForgotPasswordRequestDto request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x =>
                x.Email == request.Email);

        if (user == null)
        {
            throw new Exception(AuthMessages.UserNotFound);
        }

        var resetToken =
            Convert.ToHexString(
                RandomNumberGenerator.GetBytes(AppConstants.ByteNumber));

        user.PasswordResetToken = resetToken;

        user.ResetTokenExpires =
            DateTime.UtcNow.AddMinutes(AppConstants.LockoutMinutes);

        await _context.SaveChangesAsync();

        // Normally send email here

        return resetToken;
    }

    // Reset password
    public async Task<string> ResetPasswordAsync(
    ResetPasswordRequestDto request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x =>
                x.PasswordResetToken == request.Token);

        if (user == null)
        {
            throw new Exception(AuthMessages.InvalidToken);
        }

        if (user.ResetTokenExpires < DateTime.UtcNow)
        {
            throw new Exception(AuthMessages.TokenExpired);
        }

        var hashedPassword =
            BCrypt.Net.BCrypt.HashPassword(
                request.NewPassword);

        user.Password = hashedPassword;

        user.PasswordResetToken = null;

        user.ResetTokenExpires = null;

        await _context.SaveChangesAsync();

        return AuthMessages.PasswordResetSuccess;
    }

    public async Task<LoginResponseDto?> RefreshTokenAsync(
        string refreshToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x =>
                x.RefreshToken == refreshToken);

        if (user == null)
        {
            return null;
        }

        if (user.RefreshTokenExpiryTime < DateTime.UtcNow)
        {
            return null;
        }

        var newJwtToken = GenerateJwtToken(user);

        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;

        await _context.SaveChangesAsync();

        return new LoginResponseDto
        {
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Token = newJwtToken,
            RefreshToken = newRefreshToken
        };
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                user.Name),

            new Claim(
                ClaimTypes.Email,
                user.Email),

            new Claim(
                ClaimTypes.Role,
                user.Role)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                _jwtSettings.Key));

        var creds = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(AppConstants.ExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[AppConstants.ByteNumber];

        using var rng =
            RandomNumberGenerator.Create();

        rng.GetBytes(randomNumber);

        return Convert.ToBase64String(randomNumber);
    }
}