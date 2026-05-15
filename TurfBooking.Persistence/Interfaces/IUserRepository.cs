using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Persistence.Interfaces
{
    public interface IUserRepository
        : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(LoginRequestDto req);

        Task<User?> GetByPasswordResetTokenAsync(
            string token);

        Task<User?> GetByRefreshTokenAsync(
            string refreshToken);

        Task<Turf?> ValidateIdAsync(int? id);
    }
}