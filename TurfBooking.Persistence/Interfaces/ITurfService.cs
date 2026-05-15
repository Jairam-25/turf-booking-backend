using Application.DTOs;
using Application.Model;

namespace Persistence.Interfaces
{
    public interface ITurfService
    {
        Task<PagedResult<TurfResponseDto>> GetAllTurfAsync(
        TurfQueryParameters query);

        Task<TurfResponseDto> CreateTurfAsync(CreateTurfDto dto);

        Task<bool> DeleteTurfAsync(int id);
    }
}
