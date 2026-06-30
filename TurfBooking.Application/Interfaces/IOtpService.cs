using System.Threading.Tasks;
using Application.Common.Result;
using Application.DTOs;

namespace Application.Interfaces
{
    public interface IOtpService
    {
        Task<Result<string>> SendOtpAsync(SendOtpRequestDto request, CancellationToken cancellationToken = default);
        Task<Result<LoginResponseDto>> VerifyOtpAsync(VerifyOtpRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<string>> SendRegistrationOtpAsync(SendOtpRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<bool>> VerifyRegistrationOtpAsync(VerifyOtpRequestDto request, CancellationToken cancellationToken = default);
    }
}
