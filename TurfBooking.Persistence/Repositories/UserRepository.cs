using Application.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

public class UserRepository
    : GenericRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(LoginRequestDto req)
    {
        return await _context.Users
            .FirstOrDefaultAsync(x => x.Email == req.EmailOrPhone || x.PhoneNumber == req.EmailOrPhone);
    }
    public async Task<User?> GetByPasswordResetTokenAsync(
    string token)
    {
        return await _context.Users
            .FirstOrDefaultAsync(x =>
                x.PasswordResetToken == token);
    }

    public async Task<User?> GetByRefreshTokenAsync(
        string refreshToken)
    {
        return await _context.Users
            .FirstOrDefaultAsync(x =>
                x.RefreshToken == refreshToken);
    }
}