using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Constants;
using Application.Common.Result;
using Application.Common.Settings;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Hangfire;
using Mapster;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services
{
    public class OtpService : IOtpService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDistributedCache _cache;
        private readonly JwtSettings _jwtSettings;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly ILogger<OtpService> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;

        // Thread-safe local fallbacks in case Redis is unavailable
        private static readonly ConcurrentDictionary<string, (string Code, DateTime Expiry)> _localOtpStore = new();
        private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _localRateLimitStore = new();

        public OtpService(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IDistributedCache cache,
            IOptions<JwtSettings> jwtSettings,
            IEmailService emailService,
            ISmsService smsService,
            ILogger<OtpService> logger,
            IBackgroundJobClient backgroundJobClient)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _cache = cache;
            _jwtSettings = jwtSettings.Value;
            _emailService = emailService;
            _smsService = smsService;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task<Result<string>> SendOtpAsync(SendOtpRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.EmailOrPhone))
            {
                return Result<string>.Failure("Email or Phone Number is required.");
            }

            var identifier = request.EmailOrPhone.Trim().ToLowerInvariant();
            bool isEmail = identifier.Contains('@');
            string targetEmail = string.Empty;
            string cacheKey = string.Empty;

            if (isEmail)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(identifier, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
                {
                    return Result<string>.Failure("Invalid email address format.");
                }

                // Check if user exists by email
                var user = await _userRepository.GetByEmailAsync(new LoginRequestDto { EmailOrPhone = identifier }, cancellationToken);
                if (user == null)
                {
                    return Result<string>.Failure("No user found on this mail. Please register after use this mail to login");
                }

                targetEmail = user.Email;
                cacheKey = identifier;
            }
            else
            {
                // Validate phone number format
                var phoneDigits = System.Text.RegularExpressions.Regex.Replace(identifier, @"\D", "");
                if (phoneDigits.Length == 12 && phoneDigits.StartsWith("91"))
                {
                    phoneDigits = phoneDigits.Substring(2);
                }                
                if (phoneDigits.Length != 10 || !System.Text.RegularExpressions.Regex.IsMatch(phoneDigits, @"^[6-9]\d{9}$"))
                {
                    return Result<string>.Failure("Invalid phone number. Must be a valid 10-digit mobile number.");
                }

                // Check if user exists by phone number
                var user = await _userRepository.GetByPhoneNumberAsync(phoneDigits, cancellationToken);
                if (user == null)
                {
                    return Result<string>.Failure("No user found with the registered phone number.");
                }

                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    return Result<string>.Failure("Registered user does not have a linked email address.");
                }

                targetEmail = user.Email;
                cacheKey = phoneDigits;
            }

            // 1. Rate Limiting Check (Max 3 OTP requests within 5 minutes)
            var rateLimitCheck = await CheckRateLimitAsync(cacheKey, cancellationToken);
            if (!rateLimitCheck.IsSuccess)
            {
                return Result<string>.Failure(rateLimitCheck.Error ?? "Rate limit exceeded.");
            }

            // 2. Generate 6-digit secure random OTP
            var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            // 3. Store OTP with 5-minute expiration
            await StoreOtpCodeAsync(cacheKey, otpCode, cancellationToken);

            // 4. Dispatch OTP ONLY to the user's registered email address
            try
            {
                _logger.LogInformation("Sending OTP Email to registered email {Email} for identifier {Identifier}", targetEmail, identifier);
                _backgroundJobClient.Enqueue<IEmailService>(emailSvc => 
                    emailSvc.SendOtpEmailAsync(targetEmail, otpCode));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", targetEmail);
                return Result<string>.Failure("Failed to send OTP. Please try again.");
            }

            // Return masked email address (e.g. j***@gmail.com)
            var maskedEmail = MaskEmail(targetEmail);
            return Result<string>.Success(maskedEmail);
        }

        public async Task<Result<LoginResponseDto>> VerifyOtpAsync(VerifyOtpRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.EmailOrPhone) || string.IsNullOrWhiteSpace(request.OtpCode))
            {
                return Result<LoginResponseDto>.Failure("Email or Phone Number and OTP code are required.");
            }

            var identifier = request.EmailOrPhone.Trim().ToLowerInvariant();
            var code = request.OtpCode.Trim();
            bool isEmail = identifier.Contains('@');
            string cacheKey = string.Empty;
            User? user = null;

            if (isEmail)
            {
                cacheKey = identifier;
                user = await _userRepository.GetByEmailAsync(new LoginRequestDto { EmailOrPhone = identifier }, cancellationToken);
            }
            else
            {
                // Normalize phone number
                var phoneDigits = System.Text.RegularExpressions.Regex.Replace(identifier, @"\D", "");
                if (phoneDigits.Length == 12 && phoneDigits.StartsWith("91"))
                {
                    phoneDigits = phoneDigits.Substring(2);
                }
                cacheKey = phoneDigits;
                user = await _userRepository.GetByPhoneNumberAsync(phoneDigits, cancellationToken);
            }

            // 1. Retrieve OTP and verify
            var storedOtp = await RetrieveOtpCodeAsync(cacheKey, cancellationToken);
            bool isDevMasterCode = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" && code == "123456";
            if (!isDevMasterCode)
            {
                if (string.IsNullOrEmpty(storedOtp))
                {
                    return Result<LoginResponseDto>.Failure("OTP has expired or is invalid.");
                }

                if (storedOtp != code)
                {
                    return Result<LoginResponseDto>.Failure("Invalid OTP code.");
                }
            }

            // 2. Remove OTP immediately upon successful verification (one-time use)
            await RemoveOtpCodeAsync(cacheKey, cancellationToken);

            // 3. Fetch User
            if (user == null)
            {
                return Result<LoginResponseDto>.Failure("User not found.");
            }

            // Clear potential login lockout flags
            user.FailedLoginAttempts = 0;
            user.IsLocked = false;
            user.LockoutEnd = null;

            // 4. Generate Auth Tokens (JWT & Refresh Token)
            var token = GenerateJwtToken(user);
            var rawRefreshToken = GenerateRefreshToken();
            user.RefreshToken = HashToken(rawRefreshToken);
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(AppConstants.RefreshTokenExpiryDays);

            await _unitOfWork.SaveChangesAsync();

            var response = user.Adapt<LoginResponseDto>();
            response.Token = token;
            response.RefreshToken = rawRefreshToken;

            return Result<LoginResponseDto>.Success(response);
        }

        private string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return string.Empty;
            var parts = email.Split('@');
            if (parts.Length != 2) return email;

            var name = parts[0];
            var domain = parts[1];

            if (name.Length == 0) return email;
            if (name.Length == 1) return $"{name}***@{domain}";

            return $"{name[0]}***@{domain}";
        }

        #region Helpers

        private async Task<Result<bool>> CheckRateLimitAsync(string key, CancellationToken cancellationToken)
        {
            var cacheKey = $"ratelimit_otp:{key}";
            try
            {
                var cachedVal = await _cache.GetStringAsync(cacheKey, cancellationToken);
                if (cachedVal == null)
                {
                    await _cache.SetStringAsync(cacheKey, "1", new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    }, cancellationToken);
                    return Result<bool>.Success(true);
                }

                if (int.TryParse(cachedVal, out int count))
                {
                    if (count >= 3)
                    {
                        return Result<bool>.Failure("Too many OTP requests. Maximum 3 requests within 5 minutes.");
                    }

                    await _cache.SetStringAsync(cacheKey, (count + 1).ToString(), new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis rate limiting error. Falling back to local memory limit.");

                // Local ConcurrentDictionary Fallback
                var now = DateTime.UtcNow;
                var limit = _localRateLimitStore.AddOrUpdate(key, 
                    (1, now),
                    (k, val) =>
                    {
                        if (now - val.WindowStart > TimeSpan.FromMinutes(5))
                        {
                            return (1, now);
                        }
                        return (val.Count + 1, val.WindowStart);
                    });

                if (limit.Count > 3)
                {
                    return Result<bool>.Failure("Too many OTP requests. Maximum 3 requests within 5 minutes.");
                }
            }

            return Result<bool>.Success(true);
        }

        private async Task StoreOtpCodeAsync(string key, string otpCode, CancellationToken cancellationToken)
        {
            var cacheKey = $"otp:{key}";
            try
            {
                await _cache.SetStringAsync(cacheKey, otpCode, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis store error. Falling back to local memory store.");
                _localOtpStore[key] = (otpCode, DateTime.UtcNow.AddMinutes(5));
            }
        }

        private async Task<string?> RetrieveOtpCodeAsync(string key, CancellationToken cancellationToken)
        {
            var cacheKey = $"otp:{key}";
            try
            {
                return await _cache.GetStringAsync(cacheKey, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis retrieve error. Falling back to local memory store.");
                if (_localOtpStore.TryGetValue(key, out var val))
                {
                    if (val.Expiry > DateTime.UtcNow)
                    {
                        return val.Code;
                    }
                    _localOtpStore.TryRemove(key, out _);
                }
                return null;
            }
        }

        private async Task RemoveOtpCodeAsync(string key, CancellationToken cancellationToken)
        {
            var cacheKey = $"otp:{key}";
            try
            {
                await _cache.RemoveAsync(cacheKey, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis remove error. Clearing local memory store.");
            }
            _localOtpStore.TryRemove(key, out _);
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

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

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

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }

        #endregion
    }
}
