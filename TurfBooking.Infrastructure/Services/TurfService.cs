using Application.Common.Messages;
using Application.DTOs;
using Application.Interfaces;
using Application.Model;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Persistence.Interfaces;
using System.Text.Json;

namespace Infrastructure.Services;

public class TurfService(IUnitOfWork unitOfWork, IDistributedCache cache, ILogger<TurfService> logger, IUserRepository userRepository) : ITurfService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IDistributedCache _cache = cache;
    private readonly ILogger<TurfService> _logger = logger;
    private const string CachePrefix = "turfs_";
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<PagedResult<TurfResponseDto>> GetAllTurfAsync(
        TurfQueryParameters query)
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
            var cached = await _cache.GetStringAsync(cacheKey);

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
            turfQuery = turfQuery.Where(t =>
                t.Location.ToLower()
                    .Contains(query.Location.ToLower()));

        if (query.MinPrice.HasValue)
            turfQuery = turfQuery.Where(t =>
                t.PricePerHour >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            turfQuery = turfQuery.Where(t =>
                t.PricePerHour <= query.MaxPrice.Value);

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

        var totalCount = await turfQuery.CountAsync();

        var items = await turfQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new TurfResponseDto
            {
                Id = t.Id,
                Name = t.Name,
                Location = t.Location,
                PricePerHour = t.PricePerHour
            })
            .ToListAsync();

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
                });

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

    public async Task<TurfResponseDto> CreateTurfAsync(CreateTurfDto dto)
    {
        var turf = new Turf
        {
            Name = dto.Name,
            Location = dto.Location,
            PricePerHour = dto.PricePerHour
        };

        await _unitOfWork.Turfs.AddAsync(turf);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "New turf created. Id: {Id}, Name: {Name}, Location: {Location}",
            turf.Id, turf.Name, turf.Location);

        // Invalidate cache
        try
        {
            await InvalidateTurfCacheAsync();

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

        return new TurfResponseDto
        {
            Id = turf.Id,
            Name = turf.Name,
            Location = turf.Location,
            PricePerHour = turf.PricePerHour
        };
    }

    private async Task InvalidateTurfCacheAsync()
    {
        await _cache.RemoveAsync(
            $"{CachePrefix}p1_ps10_localall_min0_max0_sortid_asc");
    }

    public async Task<bool> DeleteTurfAsync(int id)
    {
        var turf = await _userRepository.ValidateIdAsync(id);

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

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Turf soft deleted successfully. Id: {Id}",
            id);

        // Invalidate Cache
        try
        {
            await InvalidateTurfCacheAsync();

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