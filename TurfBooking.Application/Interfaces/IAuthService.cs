using Application.DTOs;

namespace Application.Interfaces
{

    public interface IAuthService
    {
        Task<string> RegisterAsync(
            RegisterRequestDto request);

        Task<AuthResponseDto?> LoginAsync(
            LoginRequestDto request);

        Task<AuthResponseDto?> RefreshTokenAsync(
            string refreshToken);

        Task<string> ForgotPasswordAsync(
            ForgotPasswordRequestDto request);

        Task<string> ResetPasswordAsync(
            ResetPasswordRequestDto request);
    }
}