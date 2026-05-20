using Application.DTOs;
using Application.Model;
using Domain.Entities;
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
using System.Threading.Tasks;
using Xunit;
using Application.Interfaces;

namespace TurfBooking.Tests;

public class TurfServiceTests
{
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<ILogger<TurfService>> _mockLogger;
    private readonly Mock<IUserRepository> _mockUserRepository;

    public TurfServiceTests()
    {
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<TurfService>>();
        _mockUserRepository = new Mock<IUserRepository>();
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

    [Fact]
    public async Task CreateTurfAsync_AddsTurfToDatabase_AndReturnsResponse()
    {
        // Arrange
        var (context, unitOfWork) = CreateContextAndUnitOfWork();
        var turfService = new TurfService(unitOfWork, _mockCache.Object, _mockLogger.Object, _mockUserRepository.Object);

        var dto = new CreateTurfDto
        {
            Name = "Golden Turf",
            Location = "Downtown",
            PricePerHour = 150
        };

        // Act
        var result = await turfService.CreateTurfAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Golden Turf", result.Name);
        Assert.Equal("Downtown", result.Location);
        Assert.Equal(150, result.PricePerHour);

        var savedTurf = await context.Turfs.FindAsync(result.Id);
        Assert.NotNull(savedTurf);
        Assert.Equal("Golden Turf", savedTurf.Name);
    }

    [Fact]
    public async Task DeleteTurfAsync_WhenTurfExists_SoftDeletesTurf()
    {
        // Arrange
        var (context, unitOfWork) = CreateContextAndUnitOfWork();
        var turfService = new TurfService(unitOfWork, _mockCache.Object, _mockLogger.Object, _mockUserRepository.Object);

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
        Assert.True(result);
        Assert.True(turf.IsDeleted);
        Assert.NotNull(turf.DeletedAt);
    }

    [Fact]
    public async Task DeleteTurfAsync_WhenTurfDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var (context, unitOfWork) = CreateContextAndUnitOfWork();
        var turfService = new TurfService(unitOfWork, _mockCache.Object, _mockLogger.Object, _mockUserRepository.Object);

        // Act
        var result = await turfService.DeleteTurfAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAllTurfAsync_FiltersAndSortsCorrectly()
    {
        // Arrange
        var (context, unitOfWork) = CreateContextAndUnitOfWork();
        var turfService = new TurfService(unitOfWork, _mockCache.Object, _mockLogger.Object, _mockUserRepository.Object);

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
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount); // Only Stadium C fits Downton + Price >= 120
        var item = result.Items.First();
        Assert.Equal("Stadium C", item.Name);
    }
}
