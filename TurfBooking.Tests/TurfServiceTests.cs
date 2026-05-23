using Application.DTOs;
using Application.Model;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Persistence.Context;
using Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Application.Interfaces;

namespace TurfBooking.Tests;

public class TurfServiceTests
{
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<ILogger<TurfService>> _mockLogger;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ISlotService> _mockSlotService;

    public TurfServiceTests()
    {
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<TurfService>>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockSlotService = new Mock<ISlotService>();

        // Slot generation is a no-op in unit tests
        _mockSlotService
            .Setup(s => s.GenerateSlotsForTurfAsync(
                It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private (ApplicationDbContext Context, UnitOfWork UnitOfWork) CreateContextAndUnitOfWork()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var unitOfWork = new UnitOfWork(
            context,
            new UserRepository(context),
            new BookingRepository(context),
            new TurfRepository(context),
            new SlotRepository(context)
        );

        return (context, unitOfWork);
    }

    // ──────────────────────────────────────────────
    // CreateTurf Tests
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateTurfAsync_AddsTurfToDatabase_AndReturnsResponse()
    {
        // Arrange
        var (context, unitOfWork) = CreateContextAndUnitOfWork();
        var turfService = new TurfService(unitOfWork, _mockCache.Object, _mockLogger.Object, _mockUserRepository.Object, _mockSlotService.Object);

        var dto = new CreateTurfDto
        {
            Name = "Golden Turf",
            Location = "Downtown",
            PricePerHour = 150
        };

        // Act
        var result = await turfService.CreateTurfAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Golden Turf");
        result.Location.Should().Be("Downtown");
        result.PricePerHour.Should().Be(150);

        var savedTurf = await context.Turfs.FindAsync(result.Id);
        savedTurf.Should().NotBeNull();
        savedTurf!.Name.Should().Be("Golden Turf");
    }

    // ──────────────────────────────────────────────
    // DeleteTurf Tests
    // ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteTurfAsync_WhenTurfExists_SoftDeletesTurf()
    {
        // Arrange
        var (context, unitOfWork) = CreateContextAndUnitOfWork();
        var turfService = new TurfService(unitOfWork, _mockCache.Object, _mockLogger.Object, _mockUserRepository.Object, _mockSlotService.Object);

        var turf = new Turf
        {
            Name = "Turf to Delete",
            Location = "North",
            PricePerHour = 100,
            IsDeleted = false
        };
        await context.Turfs.AddAsync(turf);
        await context.SaveChangesAsync();

        // Act
        var result = await turfService.DeleteTurfAsync(turf.Id);

        // Assert
        result.Should().BeTrue();
        turf.IsDeleted.Should().BeTrue();
        turf.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteTurfAsync_WhenTurfDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var (context, unitOfWork) = CreateContextAndUnitOfWork();
        var turfService = new TurfService(unitOfWork, _mockCache.Object, _mockLogger.Object, _mockUserRepository.Object, _mockSlotService.Object);

        // Act
        var result = await turfService.DeleteTurfAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    // ──────────────────────────────────────────────
    // GetAllTurf — Filter & Sort Tests
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetAllTurfAsync_FiltersAndSortsCorrectly()
    {
        // Arrange
        var (context, unitOfWork) = CreateContextAndUnitOfWork();
        var turfService = new TurfService(unitOfWork, _mockCache.Object, _mockLogger.Object, _mockUserRepository.Object, _mockSlotService.Object);

        var turfs = new List<Turf>
        {
            new Turf { Name = "Stadium A", Location = "Downtown", PricePerHour = 100 },
            new Turf { Name = "Stadium B", Location = "Uptown", PricePerHour = 150 },
            new Turf { Name = "Stadium C", Location = "Downtown", PricePerHour = 200 }
        };
        await context.Turfs.AddRangeAsync(turfs);
        await context.SaveChangesAsync();

        var query = new TurfQueryParameters
        {
            Location = "Downtown",
            MinPrice = 120,
            MaxPrice = 250,
            Page = 1,
            PageSize = 10,
            SortBy = "price",
            SortOrder = "desc"
        };

        // Act
        var result = await turfService.GetAllTurfAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1); // Only Stadium C fits Downtown + Price >= 120
        var item = result.Items.First();
        item.Name.Should().Be("Stadium C");
    }

    // ──────────────────────────────────────────────
    // GetAllTurf — Cache HIT Test
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetAllTurfAsync_CacheHit_ReturnsCachedData()
    {
        // Arrange
        var (context, unitOfWork) = CreateContextAndUnitOfWork();
        var turfService = new TurfService(unitOfWork, _mockCache.Object, _mockLogger.Object, _mockUserRepository.Object, _mockSlotService.Object);

        // Prepare cached data
        var cachedResult = new PagedResult<TurfResponseDto>
        {
            Items = new List<TurfResponseDto>
            {
                new TurfResponseDto { Id = 99, Name = "Cached Turf", Location = "Redis City", PricePerHour = 500 }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        var cachedJson = JsonSerializer.Serialize(cachedResult);
        var cachedBytes = Encoding.UTF8.GetBytes(cachedJson);

        // Setup mock cache to return data (simulating cache HIT)
        _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedBytes);

        var query = new TurfQueryParameters
        {
            Page = 1,
            PageSize = 10,
            SortBy = "id",
            SortOrder = "asc"
        };

        // Act
        var result = await turfService.GetAllTurfAsync(query);

        // Assert — should return the cached data, NOT query the DB
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Cached Turf");
        result.Items.First().Location.Should().Be("Redis City");
        result.Items.First().PricePerHour.Should().Be(500);

        // Verify that SetAsync was NEVER called (no need to write back to cache)
        _mockCache.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ──────────────────────────────────────────────
    // GetAllTurf — Cache MISS Test
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetAllTurfAsync_CacheMiss_QueriesDbAndStoresInRedis()
    {
        // Arrange
        var (context, unitOfWork) = CreateContextAndUnitOfWork();
        var turfService = new TurfService(unitOfWork, _mockCache.Object, _mockLogger.Object, _mockUserRepository.Object, _mockSlotService.Object);

        // Seed DB with turf data
        var turfs = new List<Turf>
        {
            new Turf { Name = "DB Turf A", Location = "Chennai", PricePerHour = 300 },
            new Turf { Name = "DB Turf B", Location = "Chennai", PricePerHour = 400 }
        };
        await context.Turfs.AddRangeAsync(turfs);
        await context.SaveChangesAsync();

        // Setup mock cache to return null (simulating cache MISS)
        _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var query = new TurfQueryParameters
        {
            Location = "Chennai",
            Page = 1,
            PageSize = 10,
            SortBy = "id",
            SortOrder = "asc"
        };

        // Act
        var result = await turfService.GetAllTurfAsync(query);

        // Assert — data should come from InMemory DB
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.First().Name.Should().Be("DB Turf A");

        // Verify that SetAsync WAS called to store the result in Redis
        _mockCache.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
