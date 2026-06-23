using Application.Common.Constants;
using Application.Common.Messages;
using Application.Common.Settings;
using Application.DTOs;
using Mapster;
using Application.Interfaces;
using Domain.Entities;
using Hangfire;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Persistence.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Google.Apis.Auth;

namespace Infrastructure.Services;

public class AuthService(IUserRepository userRepository, IUnitOfWork unitOfWork, ITokenService tokenService, IEmailService emailService, IBackgroundJobClient backgroundJobClient) : IAuthService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IEmailService _emailService = emailService;
    private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;

    public async Task<Result<string>> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository
            .GetByEmailAsync(request.Email);

        if (existingUser != null)
        {
            return Result<string>.Failure(AuthMessages.EmailAlreadyExists);
        }

        var hashedPassword =
            BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = request.Adapt<User>();
        user.Password = hashedPassword;

        await _userRepository.AddAsync(user);

        await _unitOfWork.SaveChangesAsync();

        // Registration now returns IMMEDIATELY.
        // Hangfire picks up the job and sends email in background.
        // If email fails, Hangfire automatically retries 10 times.
        _backgroundJobClient.Enqueue<IEmailService>(emailSvc =>
            emailSvc.SendWelcomeEmailAsync(
                user.Email,
                user.Name));

        return Result<string>.Success(AuthMessages.RegisterSuccess);
    }

    public async Task<Result<LoginResponseDto>>
     LoginAsync(LoginRequestDto req, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(req.EmailOrPhone);

        if (user == null)
        {
            // Dummy verify to prevent timing attacks (user enumeration)
            BCrypt.Net.BCrypt.Verify("dummy", "$2a$11$DummyHashDummyHashDummyHashDummyHashDummyHashDummyHash");

            return Result<LoginResponseDto>.Failure(
                AuthMessages.InvalidCredentials);
        }

        if (user.Status == "Blocked" || (user.IsLocked && user.LockoutEnd > DateTime.UtcNow && user.LockoutEnd?.Year > 2050))
        {
            return Result<LoginResponseDto>.Failure("Your account has been temporarily restricted. Please contact support.");
        }
        else if (user.IsLocked && user.LockoutEnd > DateTime.UtcNow)
        {
            return Result<LoginResponseDto>.Failure(AuthMessages.LoginMaxAttempt);
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
                    DateTime.UtcNow.AddMinutes(AppConstants.LockoutMinutes);
            }

            await _unitOfWork.SaveChangesAsync();
            return Result<LoginResponseDto>.Failure(
                AuthMessages.IncorrectEmailOrPassword);
        }

        user.FailedLoginAttempts = 0;
        user.IsLocked = false;
        user.LockoutEnd = null;
        user.LastActive = DateTime.UtcNow;

        var token = _tokenService.GenerateJwtToken(user);

        // Generate raw token to send to client
        var rawRefreshToken = _tokenService.GenerateRefreshToken();

        // Store HASHED version in DB (not plain text)
        user.RefreshToken = _tokenService.HashToken(rawRefreshToken);

        user.RefreshTokenExpiryTime =
            DateTime.UtcNow.AddDays(7);

        await _unitOfWork.SaveChangesAsync();

        var response = user.Adapt<LoginResponseDto>();
        response.Token = token;
        response.RefreshToken = rawRefreshToken;

        return Result<LoginResponseDto>
            .Success(response);
    }

    // Forget password
    public async Task<Result<string>> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository
            .GetByEmailAsync(request.Email);

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

        _backgroundJobClient.Enqueue<IEmailService>(emailSvc =>
            emailSvc.SendPasswordResetEmailAsync(
                user.Email,
                user.Name,
                resetToken));

        return Result<string>.Success(
            "If an account exists, a reset link has been sent.");
    }

    // RESET PASSWORD
    // ResetPasswordAsync

    public async Task<Result<string>> ResetPasswordAsync(
        ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
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
        RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // Hash the incoming token to find matching DB record
        var hashedToken = _tokenService.HashToken(refreshToken);

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

        if (user.Status == "Blocked" || (user.IsLocked && user.LockoutEnd > DateTime.UtcNow && user.LockoutEnd?.Year > 2050))
        {
            return Result<LoginResponseDto>.Failure("Your account has been temporarily restricted. Please contact support.");
        }

        user.LastActive = DateTime.UtcNow;

        var newJwtToken = _tokenService.GenerateJwtToken(user);

        // New raw token for client
        var newRawRefreshToken = _tokenService.GenerateRefreshToken();

        // Store new hashed token in DB
        user.RefreshToken = _tokenService.HashToken(newRawRefreshToken);

        user.RefreshTokenExpiryTime =
            DateTime.UtcNow.AddDays(7);

        await _unitOfWork.SaveChangesAsync();

        var response = user.Adapt<LoginResponseDto>();
        response.Token = newJwtToken;
        response.RefreshToken = newRawRefreshToken;

        return Result<LoginResponseDto>
            .Success(response);
    }

    // GOOGLE SIGN IN
    public async Task<Result<LoginResponseDto>> GoogleSignInAsync(GoogleSignInRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);
            var user = await _userRepository.GetByEmailAsync(payload.Email);

            if (user == null)
            {
                return Result<LoginResponseDto>.Failure("Google user not registered");
            }

            if (user.Status == "Blocked" || (user.IsLocked && user.LockoutEnd > DateTime.UtcNow && user.LockoutEnd?.Year > 2050))
            {
                return Result<LoginResponseDto>.Failure("Your account has been temporarily restricted. Please contact support.");
            }

            user.LastActive = DateTime.UtcNow;

            user.FailedLoginAttempts = 0;
            user.IsLocked = false;

            var jwtToken = _tokenService.GenerateJwtToken(user);
            var newRawRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = _tokenService.HashToken(newRawRefreshToken);
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            
            await _unitOfWork.SaveChangesAsync();

            var response = user.Adapt<LoginResponseDto>();
            response.Token = jwtToken;
            response.RefreshToken = newRawRefreshToken;

            return Result<LoginResponseDto>.Success(response);
        }
        catch (InvalidJwtException)
        {
            return Result<LoginResponseDto>.Failure("Invalid Google IdToken");
        }
    }

    // LOGOUT
    public async Task<Result<string>> LogoutAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _unitOfWork.SaveChangesAsync();
        }
        
        return Result<string>.Success("Logged out successfully");
    }
}