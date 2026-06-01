using Application.DTOs;

namespace Application.Interfaces
{
    public interface IReviewService
    {
        Task<Result<ReviewResponseDto>> CreateReviewAsync(CreateReviewDto dto, int userId, CancellationToken ct = default);
        Task<Result<IEnumerable<ReviewResponseDto>>> GetReviewsByTurfAsync(int turfId, CancellationToken ct = default);
    }
}
