using Application.DTOs;


public interface IAuthService
{
    Task<Result<string>> RegisterAsync(
        RegisterRequestDto request, CancellationToken cancellationToken = default);

    Task<Result<LoginResponseDto>> LoginAsync(
        LoginRequestDto request, CancellationToken cancellationToken = default);

    Task<Result<string>> ForgotPasswordAsync(
        ForgotPasswordRequestDto request, CancellationToken cancellationToken = default);

    Task<Result<string>> ResetPasswordAsync(
        ResetPasswordRequestDto request, CancellationToken cancellationToken = default);

    Task<Result<LoginResponseDto>> RefreshTokenAsync(
        string refreshToken, CancellationToken cancellationToken = default);
}