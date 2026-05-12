using Application.DTOs;

namespace Application.Interfaces
{

    public interface IAuthService
    {
        Task<string> RegisterAsync(
            RegisterRequestDto request);

        Task<LoginResponseDto?> LoginAsync(
            LoginRequestDto request);

        Task<LoginResponseDto?> RefreshTokenAsync(
            string refreshToken);

        Task<string> ForgotPasswordAsync(
            ForgotPasswordRequestDto request);

        Task<string> ResetPasswordAsync(
            ResetPasswordRequestDto request);
    }
}