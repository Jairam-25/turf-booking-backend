using Domain.Entities;

namespace TurfBooking.Persistence.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);

    Task AddAsync(User user);
}