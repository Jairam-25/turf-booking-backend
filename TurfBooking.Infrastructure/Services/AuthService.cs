using Application.Common.Constants;
using Application.Common.Messages;
using Application.Common.Settings;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services;

public class AuthService(IUserRepository userRepository, IUnitOfWork unitOfWork, IOptions<JwtSettings> jwtSettings, IEmailService emailService) : IAuthService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;
    private readonly IEmailService _emailService = emailService;    

    public async Task<Result<string>> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await _userRepository
            .GetByEmailAsync(new LoginRequestDto { EmailOrPhone = request.Email });

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

        await _userRepository.AddAsync(user);

        await _unitOfWork.SaveChangesAsync();

        // Sended mail after registeration on Registered mail I'd or mobile number.
        await _emailService.SendWelcomeEmailAsync
        (
            user.Email,
            user.Name
        );

        return Result<string>.Success(AuthMessages.RegisterSuccess);
    }

    public async Task<Result<LoginResponseDto>>
     LoginAsync(LoginRequestDto req)
    {
        var user = await _userRepository.GetByEmailAsync(req);

        if (user == null)
        {
            return Result<LoginResponseDto>.Failure(
                AuthMessages.InvalidCredentials);
        }

        if (user.IsLocked && user.LockoutEnd > DateTime.UtcNow)
        {
            throw new Exception(AuthMessages.LoginMaxAttempt);
        }

        var isPasswordValid =
            BCrypt.Net.BCrypt.Verify(
                req.Password,
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

            await _unitOfWork.SaveChangesAsync();
            return Result<LoginResponseDto>.Failure(
                AuthMessages.IncorrectEmailOrPassword);
        }

        user.FailedLoginAttempts = 0;
        user.IsLocked = false;

        var token = GenerateJwtToken(user);

        // Generate raw token to send to client
        var rawRefreshToken = GenerateRefreshToken();

        // Store HASHED version in DB (not plain text)
        user.RefreshToken = HashToken(rawRefreshToken);

        user.RefreshTokenExpiryTime =
            DateTime.UtcNow.AddDays(7);

        await _unitOfWork.SaveChangesAsync();

        var response = new LoginResponseDto
        {
            Name = user.Name,
            Email = user.Email,
            Number = user.PhoneNumber,
            Role = user.Role,
            Token = token,
            // Return RAW token to client (not hashed)
            RefreshToken = rawRefreshToken
        };

        return Result<LoginResponseDto>
            .Success(response);
    }

    // Forget password
    public async Task<Result<string>> ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        var user = await _userRepository
            .GetByEmailAsync(new LoginRequestDto { EmailOrPhone = request.Email });

        if (user == null)
        {
            return Result<string>.Failure(
                AuthMessages.UserNotFound);
        }

        var resetToken = Guid.NewGuid().ToString();

        user.PasswordResetToken = resetToken;
        user.ResetTokenExpires =
            DateTime.UtcNow.AddMinutes(30);

        await _unitOfWork.SaveChangesAsync();

        return Result<string>.Success(
            resetToken);
    }

    // RESET PASSWORD
    // ResetPasswordAsync

    public async Task<Result<string>> ResetPasswordAsync(
        ResetPasswordRequestDto request)
    {
        var user = await _userRepository
            .GetByPasswordResetTokenAsync(
                request.Token);

        if (user == null)
        {
            return Result<string>.Failure(
                AuthMessages.InvalidToken);
        }

        if (user.ResetTokenExpires < DateTime.UtcNow)
        {
            return Result<string>.Failure(
                AuthMessages.TokenExpired);
        }

        user.Password =
            BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        user.PasswordResetToken = null;
        user.ResetTokenExpires = null;

        await _unitOfWork.SaveChangesAsync();

        return Result<string>.Success(
            AuthMessages.PasswordResetSuccess);
    }

    // REFRESH TOKEN
    // RefreshTokenAsync

    public async Task<Result<LoginResponseDto>>
        RefreshTokenAsync(string refreshToken)
    {
        // Hash the incoming token to find matching DB record
        var hashedToken = HashToken(refreshToken);

        var user = await _userRepository
            .GetByRefreshTokenAsync(hashedToken); // Compare with hashed value

        if (user == null)
        {
            return Result<LoginResponseDto>.Failure(
                AuthMessages.InvalidToken);
        }

        if (user.RefreshTokenExpiryTime < DateTime.UtcNow)
        {
            return Result<LoginResponseDto>.Failure(
                AuthMessages.TokenExpired);
        }

        var newJwtToken = GenerateJwtToken(user);

        // New raw token for client
        var newRawRefreshToken = GenerateRefreshToken();

        // Store new hashed token in DB
        user.RefreshToken = HashToken(newRawRefreshToken);

        user.RefreshTokenExpiryTime =
            DateTime.UtcNow.AddDays(7);

        await _unitOfWork.SaveChangesAsync();

        var response = new LoginResponseDto
        {
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Token = newJwtToken,
            // Send raw token back to client
            RefreshToken = newRawRefreshToken
        };

        return Result<LoginResponseDto>
            .Success(response);
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name,           user.Name),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimTypes.Role,           user.Role)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.Key));

        var creds = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(AppConstants.ExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[AppConstants.ByteNumber];

        using var rng = RandomNumberGenerator.Create();

        rng.GetBytes(randomNumber);

        return Convert.ToBase64String(randomNumber);
    }

    // --------------------------------------------------------
    // METHOD — Hash token using SHA256
    // WHY SHA256 : Unlike BCrypt, SHA256 is deterministic so
    // we can hash the incoming token and query it in the DB.
    // BCrypt generates different hashes each time (random salt)
    // making DB lookup impossible — SHA256 solves this.
    // --------------------------------------------------------
    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(bytes);
    }
}