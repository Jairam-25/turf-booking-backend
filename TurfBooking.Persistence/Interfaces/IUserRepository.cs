using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

public interface IUserRepository
    : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(LoginRequestDto req);

    Task<User?> GetByPasswordResetTokenAsync(
        string token);

    Task<User?> GetByRefreshTokenAsync(
        string refreshToken);
}