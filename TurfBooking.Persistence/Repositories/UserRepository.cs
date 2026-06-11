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

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var input = email.Trim();
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
                    (x.PhoneNumber != null && x.PhoneNumber.EndsWith(suffix)), cancellationToken);
        }

        public async Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return null;

            var input = phoneNumber.Trim();
            var suffix = input;
            if (input.Length >= 10)
            {
                suffix = input.Substring(input.Length - 10);
            }

            return await _context.Users
                .FirstOrDefaultAsync(x => 
                    x.PhoneNumber == input || 
                    x.PhoneNumber == suffix || 
                    (x.PhoneNumber != null && x.PhoneNumber.EndsWith(suffix)), cancellationToken);
        }

        public async Task<User?> GetByPasswordResetTokenAsync(
        string token, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.PasswordResetToken == token, cancellationToken);
        }

        public async Task<User?> GetByRefreshTokenAsync(
            string refreshToken, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.RefreshToken == refreshToken, cancellationToken);
        }
    }
}
