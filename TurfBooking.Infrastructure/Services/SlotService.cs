using Application.Common.Result;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class SlotService : ISlotService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SlotService> _logger;
        private readonly Microsoft.Extensions.Caching.Distributed.IDistributedCache _cache;
        private readonly Hangfire.IBackgroundJobClient _backgroundJobClient;

        // Slots run from 6 AM to 10 PM (exclusive), 1-hour each → 16 slots/day
        private const int SlotStartHour = 6;
        private const int SlotEndHour = 22;

        public SlotService(IUnitOfWork unitOfWork, ILogger<SlotService> logger, Microsoft.Extensions.Caching.Distributed.IDistributedCache cache, Hangfire.IBackgroundJobClient backgroundJobClient)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _cache = cache;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task<Result<IEnumerable<SlotResponseDto>>> GetAvailableSlotsAsync(int turfId, CancellationToken ct = default)
        {
            var cacheKey = $"slots_turf_{turfId}";
            try
            {
                var cached = await _cache.GetStringAsync(cacheKey, ct);
                if (cached != null)
                {
                    _logger.LogInformation("Cache HIT for slots of Turf {TurfId}", turfId);
                    var cachedSlots = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<SlotResponseDto>>(cached);
                    if (cachedSlots != null) return Result<IEnumerable<SlotResponseDto>>.Success(cachedSlots);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis unavailable while reading slots cache.");
            }

            // Verify if the turf exists and is not deleted
            var turfExists = await _unitOfWork.Turfs.AsQueryable()
                .AnyAsync(t => t.Id == turfId && !t.IsDeleted, ct);
            if (!turfExists)
            {
                return Result<IEnumerable<SlotResponseDto>>.Failure($"Turf with ID {turfId} not found.");
            }

            var today = DateTime.UtcNow.Date;
            
            // Check if there are slots generated for today onwards
            var hasSlots = await _unitOfWork.Slots.AsQueryable()
                .AnyAsync(s => s.TurfId == turfId && s.StartTime >= today, ct);
                
            if (!hasSlots)
            {
                // Offload slot generation to Hangfire to ensure instant load under 2 seconds!
                _logger.LogInformation("Enqueuing background job to generate slots for Turf {TurfId}", turfId);
                _backgroundJobClient.Enqueue<ISlotService>(svc => svc.GenerateSlotsForAllTurfsAsync(7, CancellationToken.None));
                return Result<IEnumerable<SlotResponseDto>>.Success(new List<SlotResponseDto>());
            }

            var slots = await _unitOfWork.Slots.AsQueryable()
                .Where(s => s.TurfId == turfId)
                .Select(s => new SlotResponseDto
                {
                    SlotId = s.Id,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    TurfId = s.TurfId,
                    IsBooked = s.IsBooked
                })
                .ToListAsync(ct);

            try
            {
                await _cache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(slots), new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis unavailable while writing slots cache.");
            }

            return Result<IEnumerable<SlotResponseDto>>.Success(slots);
        }

        public async Task GenerateSlotsForTurfAsync(int turfId, DateTime date, CancellationToken ct = default)
        {
            // Build the date's midnight in UTC
            var dayStart = date.Date;

            // Fetch existing StartTimes for this turf on this day to avoid duplicates
            var existingStartTimes = (await _unitOfWork.Slots.AsQueryable()
                .Where(s => s.TurfId == turfId
                         && s.StartTime >= dayStart
                         && s.StartTime < dayStart.AddDays(1))
                .Select(s => s.StartTime)
                .ToListAsync(ct))
                .ToHashSet();

            var newSlots = new List<Slot>();

            for (int hour = SlotStartHour; hour < SlotEndHour; hour++)
            {
                var startTime = dayStart.AddHours(hour);
                if (existingStartTimes.Contains(startTime))
                    continue; // already exists — skip

                newSlots.Add(new Slot
                {
                    TurfId = turfId,
                    StartTime = startTime,
                    EndTime = startTime.AddHours(1),
                    IsBooked = false
                });
            }

            if (newSlots.Count == 0)
            {
                _logger.LogDebug(
                    "No new slots to generate for TurfId={TurfId} on {Date} (all already exist).",
                    turfId, dayStart.ToString("yyyy-MM-dd"));
                return;
            }

            // Add each slot via the repository, then save once
            foreach (var slot in newSlots)
                await _unitOfWork.Slots.AddAsync(slot, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Generated {Count} slots for TurfId={TurfId} on {Date}.",
                newSlots.Count, turfId, dayStart.ToString("yyyy-MM-dd"));
        }

        public async Task GenerateSlotsForAllTurfsAsync(int days = 7, CancellationToken ct = default)
        {
            var turfIds = await _unitOfWork.Turfs.AsQueryable()
                .Select(t => t.Id)
                .ToListAsync(ct);

            _logger.LogInformation(
                "Starting slot generation for {TurfCount} turfs over {Days} days.",
                turfIds.Count, days);

            var today = DateTime.UtcNow.Date;

            foreach (var turfId in turfIds)
            {
                for (int d = 0; d < days; d++)
                {
                    await GenerateSlotsForTurfAsync(turfId, today.AddDays(d), ct);
                }
            }

            _logger.LogInformation("Slot generation complete.");
        }
    }
}
