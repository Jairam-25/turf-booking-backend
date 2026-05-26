using Application.Common.Messages;
using Application.DTOs;
using Mapster;
using Application.Interfaces;
using Application.Model;
using Domain.Entities;
using Domain.Specifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Persistence.Interfaces;
using Persistence.Specifications;
using System.Text.Json;

namespace Infrastructure.Services;

public class TurfService(IUnitOfWork unitOfWork, IDistributedCache cache, ILogger<TurfService> logger, IUserRepository userRepository, ISlotService slotService) : ITurfService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IDistributedCache _cache = cache;
    private readonly ILogger<TurfService> _logger = logger;
    private const string CachePrefix = "turfs_";
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ISlotService _slotService = slotService;

    public async Task<PagedResult<TurfResponseDto>> GetAllTurfAsync(
        TurfQueryParameters query, CancellationToken cancellationToken = default)
    {
        var cacheKey =
            $"{CachePrefix}" +
            $"p{query.Page}_ps{query.PageSize}_" +
            $"loc{query.Location ?? "all"}_" +
            $"min{query.MinPrice ?? 0}_" +
            $"max{query.MaxPrice ?? 0}_" +
            $"sort{query.SortBy ?? "id"}_{query.SortOrder}";

        // Try Redis first
        try
        {
            var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (cached != null)
            {
                _logger.LogInformation(
                    "Cache HIT — Turf list served from Redis. Key: {Key}",
                    cacheKey);

                return JsonSerializer
                    .Deserialize<PagedResult<TurfResponseDto>>(cached)!;
            }

            _logger.LogInformation(
                "Cache MISS — Key not found in Redis: {Key}",
                cacheKey);
        }
        catch (Exception ex)
        {
            // Redis is down — log warning and fall through to DB
            _logger.LogWarning(
                ex,
                "Redis unavailable while reading cache. " +
                "Falling back to database. Key: {Key}",
                cacheKey);
        }

        // Query DB
        var turfQuery = _unitOfWork.Turfs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var locationSpec = new TurfByLocationSpec(query.Location);
            turfQuery = SpecificationEvaluator<Turf>.GetQuery(turfQuery, locationSpec);
        }

        if (query.MinPrice.HasValue || query.MaxPrice.HasValue)
        {
            var priceRangeSpec = new TurfByPriceRangeSpec(query.MinPrice, query.MaxPrice);
            turfQuery = SpecificationEvaluator<Turf>.GetQuery(turfQuery, priceRangeSpec);
        }

        turfQuery = query.SortBy?.ToLower() switch
        {
            "price" => query.SortOrder == "desc"
                ? turfQuery.OrderByDescending(t => t.PricePerHour)
                : turfQuery.OrderBy(t => t.PricePerHour),

            "name" => query.SortOrder == "desc"
                ? turfQuery.OrderByDescending(t => t.Name)
                : turfQuery.OrderBy(t => t.Name),

            _ => turfQuery.OrderBy(t => t.Id)
        };

        var totalCount = await turfQuery.CountAsync(cancellationToken);

        var items = await turfQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ProjectToType<TurfResponseDto>()
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Turf list fetched from DB. " +
            "Total: {Total}, Page: {Page}, PageSize: {PageSize}",
            totalCount, query.Page, query.PageSize);

        var result = new PagedResult<TurfResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };

        // Store in Redis
        try
        {
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(result),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromMinutes(5)
                }, cancellationToken);

            _logger.LogInformation(
                "Turf list cached in Redis for 5 minutes. Key: {Key}",
                cacheKey);
        }
        catch (Exception ex)
        {
            // Redis is down — data already fetched, just skip caching
            _logger.LogWarning(
                ex,
                "Redis unavailable while writing cache. " +
                "Response will be returned without caching. Key: {Key}",
                cacheKey);
        }

        return result;
    }

    public async Task<TurfResponseDto> CreateTurfAsync(CreateTurfDto dto, CancellationToken cancellationToken = default)
    {
        var turf = dto.Adapt<Turf>();

        await _unitOfWork.Turfs.AddAsync(turf, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "New turf created. Id: {Id}, Name: {Name}, Location: {Location}",
            turf.Id, turf.Name, turf.Location);

        // Auto-generate 7 days of bookable slots for the new turf
        try
        {
            var today = DateTime.UtcNow.Date;
            for (int d = 0; d < 7; d++)
                await _slotService.GenerateSlotsForTurfAsync(turf.Id, today.AddDays(d), cancellationToken);

            _logger.LogInformation(
                "Auto-generated 7 days of slots for new turf Id: {Id}", turf.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to auto-generate slots for new turf Id: {Id}. "
                + "Slots can be generated by the daily background job.", turf.Id);
        }

        // Invalidate cache
        try
        {
            await InvalidateTurfCacheAsync(cancellationToken);

            _logger.LogInformation(
                "Turf cache invalidated after new turf created. " +
                "Id: {Id}", turf.Id);
        }
        catch (Exception ex)
        {
            // Cache invalidation failed — not critical, log and continue
            _logger.LogWarning(
                ex,
                "Failed to invalidate turf cache after creating " +
                "turf Id: {Id}. Old cached data may be served " +
                "until it expires.", turf.Id);
        }

        return turf.Adapt<TurfResponseDto>();
    }

    public async Task<TurfResponseDto?> GetTurfByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var turf = await _unitOfWork.Turfs.AsQueryable()
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);

        if (turf == null)
        {
            _logger.LogWarning(
                "Turf not found. Id: {Id}",
                id);

            return null;
        }

        return turf.Adapt<TurfResponseDto>();
    }

    private async Task InvalidateTurfCacheAsync(CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(
            $"{CachePrefix}p1_ps10_localall_min0_max0_sortid_asc", cancellationToken);
    }

    public async Task<bool> DeleteTurfAsync(int id, CancellationToken cancellationToken = default)
    {
        var turf = await _unitOfWork.Turfs.AsQueryable()
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);

        if (turf == null)
        {
            _logger.LogWarning(
                "Delete failed. Turf not found. Id: {Id}",
                id);

            return false;
        }

        // Soft Delete
        turf.IsDeleted = true;
        turf.DeletedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Turf soft deleted successfully. Id: {Id}",
            id);

        // Invalidate Cache
        try
        {
            await InvalidateTurfCacheAsync(cancellationToken);

            _logger.LogInformation(
                "Turf cache invalidated after delete. Id: {Id}",
                id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to invalidate turf cache after delete. Id: {Id}",
                id);
        }

        return true;
    }

}