using Application.Common.Result;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class SlotService : ISlotService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SlotService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<IEnumerable<SlotResponseDto>>> GetAvailableSlotsAsync(int turfId, CancellationToken ct = default)
        {
            var slots = await _unitOfWork.Slots.AsQueryable()
                .Where(s => s.TurfId == turfId && !s.IsBooked)
                .Select(s => new SlotResponseDto
                {
                    SlotId = s.Id,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    TurfId = s.TurfId
                })
                .ToListAsync(ct);

            return Result<IEnumerable<SlotResponseDto>>.Success(slots);
        }
    }
}
