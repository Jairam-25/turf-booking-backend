using Application.Common.Result;
using Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IBookingService
    {
        Task<Result<object>> BookSlotAsync(CreateBookingDto dto, int userId, CancellationToken ct = default);
        Task<Result<object>> GetMyBookingsAsync(int userId, CancellationToken ct = default);
        Task<Result<string>> CancelBookingAsync(int bookingId, int userId, CancellationToken ct = default);
    }
}
