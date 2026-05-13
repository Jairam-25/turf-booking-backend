// PAGINATION & FILTERING
// Get() now accepts TurfQueryParameters for
// filtering by location, price and pagination.
// Returns PagedResult<Turf> instead of plain list.

using Application.DTOs;
using Application.Interfaces;
using Application.Model;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TurfController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    // Inject DbContext for direct query building
    // (until you move this into a proper TurfRepository)
    private readonly ApplicationDbContext _context;

    public TurfController(
        IUnitOfWork unitOfWork,
        ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    // Accept query parameters from URL
    // Example: GET /api/turf?page=1&pageSize=5&location=Chennai&maxPrice=500
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] TurfQueryParameters query)
    {
        // Start with full query
        var turfQuery = _context.Turfs.AsQueryable();

        // FILTER : by location (case-insensitive contains)
        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            turfQuery = turfQuery.Where(t =>
                t.Location.ToLower()
                    .Contains(query.Location.ToLower()));
        }

        // FILTER : by minimum price
        if (query.MinPrice.HasValue)
        {
            turfQuery = turfQuery.Where(t =>
                t.PricePerHour >= query.MinPrice.Value);
        }

        // FILTER : by maximum price
        if (query.MaxPrice.HasValue)
        {
            turfQuery = turfQuery.Where(t =>
                t.PricePerHour <= query.MaxPrice.Value);
        }

        // SORT : by chosen field
        turfQuery = query.SortBy?.ToLower() switch
        {
            "price" => query.SortOrder == "desc"
                ? turfQuery.OrderByDescending(t => t.PricePerHour)
                : turfQuery.OrderBy(t => t.PricePerHour),

            "name" => query.SortOrder == "desc"
                ? turfQuery.OrderByDescending(t => t.Name)
                : turfQuery.OrderBy(t => t.Name),

            // Default sort by Id
            _ => turfQuery.OrderBy(t => t.Id)
        };

        // Get total count before pagination
        var totalCount = await turfQuery.CountAsync();

        // PAGINATE : Skip and Take
        var items = await turfQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        // Build and return paged result
        var result = new PagedResult<Turf>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTurfDto dto)
    {
        var turf = new Turf
        {
            Name = dto.Name,
            Location = dto.Location,
            PricePerHour = dto.PricePerHour
        };

        await _unitOfWork.Turfs.AddAsync(turf);
        await _unitOfWork.SaveChangesAsync();

        return Ok(turf);
    }
}

// ============================================================
// EXAMPLE API CALLS :
//
// All turfs (page 1, default 10 per page):
//   GET /api/turf
//
// Filter by location:
//   GET /api/turf?location=Chennai
//
// Filter by price range:
//   GET /api/turf?minPrice=200&maxPrice=800
//
// Sort by price descending:
//   GET /api/turf?sortBy=price&sortOrder=desc
//
// Full example with pagination:
//   GET /api/turf?page=2&pageSize=5&location=Madurai&maxPrice=600
//
// EXAMPLE RESPONSE :
// {
//   "items": [ { "id": 1, "name": "Green Turf", ... } ],
//   "totalCount": 42,
//   "page": 2,
//   "pageSize": 5,
//   "totalPages": 9,
//   "hasNextPage": true,
//   "hasPreviousPage": true
// }
// ============================================================