using Application.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Application.Interfaces;

namespace Persistence.Repositories
{
    public class UserRepository 
        : GenericRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(LoginRequestDto req)
        {
            if (string.IsNullOrWhiteSpace(req.EmailOrPhone))
                return null;

            var input = req.EmailOrPhone.Trim();

            // Extract the last 10 digits as suffix for robust matching
            var suffix = input;
            if (input.Length >= 10)
            {
                suffix = input.Substring(input.Length - 10);
            }

            return await _context.Users
                .FirstOrDefaultAsync(x => 
                    x.Email == input || 
                    x.PhoneNumber == input || 
                    x.PhoneNumber == suffix || 
                    (x.PhoneNumber != null && x.PhoneNumber.EndsWith(suffix)));
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
}
