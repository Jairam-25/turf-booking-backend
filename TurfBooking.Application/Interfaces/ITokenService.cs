using Domain.Entities;

namespace Application.Interfaces;

public interface ITokenService
{
    string GenerateJwtToken(User user);
    string GenerateRefreshToken();
    string HashToken(string token);
}
