using Application.DTOs;
using Application.Model;

namespace Persistence.Interfaces
{
    public interface ITurfService
    {
        Task<PagedResult<TurfResponseDto>> GetAllTurfAsync(
        TurfQueryParameters query, CancellationToken cancellationToken = default);

        Task<TurfResponseDto> CreateTurfAsync(CreateTurfDto dto, CancellationToken cancellationToken = default);

        Task<TurfResponseDto?> GetTurfByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> DeleteTurfAsync(int id, CancellationToken cancellationToken = default);
    }
}
