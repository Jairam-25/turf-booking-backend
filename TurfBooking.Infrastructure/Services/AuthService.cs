using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Persistence.Context;

namespace Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<string> RegisterAsync(
        RegisterRequestDto request)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(x =>
                x.Email == request.Email);

        if (existingUser != null)
        {
            throw new Exception("Email already exists");
        }

        var hashedPassword =
            BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Password = hashedPassword
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        return "User registered successfully";
    }

    public async Task<AuthResponseDto?> LoginAsync(
        LoginRequestDto request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x =>
                x.Email == request.Email);

        if (user == null)
        {
            return null;
        }

        if (user.IsLocked &&
            user.LockoutEnd > DateTime.UtcNow)
        {
            throw new Exception(
                "You entered the wrong password more than 5 times. So account temporarily locked for 15 minutes.");
        }

        var isPasswordValid =
            BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.Password);

        if (!isPasswordValid)
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= 5)
            {
                user.IsLocked = true;

                user.LockoutEnd =
                    DateTime.Now.AddMinutes(5);
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
            DateTime.UtcNow.AddDays(7);

        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            Name = user.Name,
            Email = user.Email,
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
            throw new Exception("User not found");
        }

        var resetToken =
            Convert.ToHexString(
                RandomNumberGenerator.GetBytes(64));

        user.PasswordResetToken = resetToken;

        user.ResetTokenExpires =
            DateTime.UtcNow.AddMinutes(15);

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
            throw new Exception("Invalid token");
        }

        if (user.ResetTokenExpires < DateTime.UtcNow)
        {
            throw new Exception("Token expired");
        }

        var hashedPassword =
            BCrypt.Net.BCrypt.HashPassword(
                request.NewPassword);

        user.Password = hashedPassword;

        user.PasswordResetToken = null;

        user.ResetTokenExpires = null;

        await _context.SaveChangesAsync();

        return "Password reset successful";
    }

    public async Task<AuthResponseDto?> RefreshTokenAsync(
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

        return new AuthResponseDto
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
                _configuration["Jwt:Key"]!));

        var creds = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];

        using var rng =
            RandomNumberGenerator.Create();

        rng.GetBytes(randomNumber);

        return Convert.ToBase64String(randomNumber);
    }
}