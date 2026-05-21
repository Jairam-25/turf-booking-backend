using Application.Common.Result;
using Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ISlotService
    {
        Task<Result<IEnumerable<SlotResponseDto>>> GetAvailableSlotsAsync(int turfId, CancellationToken ct = default);
    }
}
