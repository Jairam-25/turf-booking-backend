using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IUserRepository
        : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(LoginRequestDto req, CancellationToken cancellationToken = default);

        Task<User?> GetByPasswordResetTokenAsync(
            string token, CancellationToken cancellationToken = default);

        Task<User?> GetByRefreshTokenAsync(
            string refreshToken, CancellationToken cancellationToken = default);
    }
}