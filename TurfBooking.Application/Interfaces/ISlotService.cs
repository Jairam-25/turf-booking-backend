using Application.Common.Result;
using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ISlotService
    {
        Task<Result<IEnumerable<SlotResponseDto>>> GetAvailableSlotsAsync(int turfId, CancellationToken ct = default);

        /// <summary>
        /// Generates 1-hour slots (6 AM – 10 PM) for the given turf on the given date.
        /// Skips slots that already exist (idempotent).
        /// </summary>
        Task GenerateSlotsForTurfAsync(int turfId, DateTime date, CancellationToken ct = default);

        /// <summary>
        /// Generates slots for all active turfs for the next <paramref name="days"/> days.
        /// Called on startup to backfill existing turfs and on daily timer.
        /// </summary>
        Task GenerateSlotsForAllTurfsAsync(int days = 7, CancellationToken ct = default);
    }
}
