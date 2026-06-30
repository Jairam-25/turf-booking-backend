const fs = require('fs');

// 1. Update IOtpService.cs
let interfacePath = 'TurfBooking.Application/Interfaces/IOtpService.cs';
if (fs.existsSync(interfacePath)) {
    let content = fs.readFileSync(interfacePath, 'utf8');
    if (!content.includes('SendRegistrationOtpAsync')) {
        content = content.replace('Task<Result<LoginResponseDto>> VerifyOtpAsync(VerifyOtpRequestDto request, CancellationToken cancellationToken = default);',
            'Task<Result<LoginResponseDto>> VerifyOtpAsync(VerifyOtpRequestDto request, CancellationToken cancellationToken = default);\n    Task<Result<string>> SendRegistrationOtpAsync(SendOtpRequestDto request, CancellationToken cancellationToken = default);\n    Task<Result<bool>> VerifyRegistrationOtpAsync(VerifyOtpRequestDto request, CancellationToken cancellationToken = default);');
        fs.writeFileSync(interfacePath, content);
        console.log('Updated IOtpService.cs');
    }
}

// 2. Update OtpService.cs
let servicePath = 'TurfBooking.Infrastructure/Services/OtpService.cs';
if (fs.existsSync(servicePath)) {
    let content = fs.readFileSync(servicePath, 'utf8');
    if (!content.includes('public async Task<Result<string>> SendRegistrationOtpAsync')) {
        
        const newMethods = `
        public async Task<Result<string>> SendRegistrationOtpAsync(SendOtpRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.EmailOrPhone)) return Result<string>.Failure("Email is required.");
            var identifier = request.EmailOrPhone.Trim().ToLowerInvariant();
            if (!identifier.Contains('@')) return Result<string>.Failure("Please provide a valid email address for registration.");
            if (!System.Text.RegularExpressions.Regex.IsMatch(identifier, @"^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$")) return Result<string>.Failure("Invalid email format.");

            var user = await _userRepository.GetByEmailAsync(identifier, cancellationToken);
            if (user != null) return Result<string>.Failure("User already exists. Please login instead.");

            var rateLimitCheck = await CheckRateLimitAsync(identifier, cancellationToken);
            if (!rateLimitCheck.IsSuccess) return Result<string>.Failure(rateLimitCheck.Error ?? "Rate limit exceeded.");

            var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            await StoreOtpCodeAsync(identifier, otpCode, cancellationToken);

            try
            {
                _backgroundJobClient.Enqueue<IEmailService>(emailSvc => emailSvc.SendOtpEmailAsync(identifier, otpCode));
            }
            catch (Exception ex)
            {
                return Result<string>.Failure("Failed to send OTP email.");
            }

            return Result<string>.Success("OTP sent successfully to email.");
        }

        public async Task<Result<bool>> VerifyRegistrationOtpAsync(VerifyOtpRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.EmailOrPhone) || string.IsNullOrWhiteSpace(request.OtpCode))
                return Result<bool>.Failure("Email and OTP are required.");
            
            var identifier = request.EmailOrPhone.Trim().ToLowerInvariant();
            var storedOtp = await RetrieveOtpCodeAsync(identifier, cancellationToken);
            
            bool isDevMasterCode = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" && request.OtpCode.Trim() == "123456";
            if (!isDevMasterCode)
            {
                if (string.IsNullOrEmpty(storedOtp)) return Result<bool>.Failure("OTP expired or invalid.");
                if (storedOtp != request.OtpCode.Trim()) return Result<bool>.Failure("Invalid OTP code.");
            }
            
            await RemoveOtpCodeAsync(identifier, cancellationToken);
            return Result<bool>.Success(true);
        }
`;
        content = content.replace('#region Helpers', newMethods + '\n        #region Helpers');
        fs.writeFileSync(servicePath, content);
        console.log('Updated OtpService.cs');
    }
}

// 3. Update AuthController.cs
let controllerPath = 'TurfBooking.API/Controllers/AuthController.cs';
if (fs.existsSync(controllerPath)) {
    let content = fs.readFileSync(controllerPath, 'utf8');
    if (!content.includes('send-registration-otp')) {
        const endpoints = `
        [HttpPost("send-registration-otp")]
        public async Task<IActionResult> SendRegistrationOtp(SendOtpRequestDto request)
        {
            var result = await _otpService.SendRegistrationOtpAsync(request);
            if (!result.IsSuccess) return BadRequest(ApiResponse<object>.FailureResponse(result.Error ?? "Failed", null, 400));
            return Ok(ApiResponse<string>.SuccessResponse(result.Value ?? "", "OTP sent"));
        }

        [HttpPost("verify-registration-otp")]
        public async Task<IActionResult> VerifyRegistrationOtp(VerifyOtpRequestDto request)
        {
            var result = await _otpService.VerifyRegistrationOtpAsync(request);
            if (!result.IsSuccess) return BadRequest(ApiResponse<object>.FailureResponse(result.Error ?? "Verification failed", null, 400));
            return Ok(ApiResponse<bool>.SuccessResponse(result.Value, "Verified successfully"));
        }
`;
        content = content.replace('[HttpPost("register")]', endpoints + '\n        [HttpPost("register")]');
        fs.writeFileSync(controllerPath, content);
        console.log('Updated AuthController.cs');
    }
}
