using Application.DTOs;


public interface IAuthService
{
    Task<Result<string>> RegisterAsync(
        RegisterRequestDto request);

    Task<Result<LoginResponseDto>> LoginAsync(
        LoginRequestDto request);

    Task<Result<string>> ForgotPasswordAsync(
        ForgotPasswordRequestDto request);

    Task<Result<string>> ResetPasswordAsync(
        ResetPasswordRequestDto request);

    Task<Result<LoginResponseDto>> RefreshTokenAsync(
        string refreshToken);
}