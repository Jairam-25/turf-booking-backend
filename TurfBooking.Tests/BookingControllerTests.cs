using Application.Common.Result;
using Application.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Repositories;
using System.Collections;
using System.Security.Claims;
using TurfBooking.API.Controllers;

namespace TurfBooking.Tests;

public class BookingControllerTests
{
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

    private void SetupAuthenticatedUser(ControllerBase controller, string userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "mock");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task BookSlot_WithValidAvailableSlot_CreatesBookingAndMarksSlotAsBooked()
    {
        // Arrange
        var (context, unitOfWork) = CreateContextAndUnitOfWork();
        var controller = new BookingController(unitOfWork);
        SetupAuthenticatedUser(controller, "100");

        var turf = new Turf { Id = 1, Name = "Champion Field", Location = "Sector 5" };
        var slot = new Slot { Id = 10, TurfId = 1, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(1), IsBooked = false, Turf = turf };
        await context.Turfs.AddAsync(turf);
        await context.Slots.AddAsync(slot);
        await context.SaveChangesAsync();

        var dto = new CreateBookingDto { SlotId = 10 };

        // Act
        var response = await controller.BookSlot(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.True(slot.IsBooked);

        var booking = await context.Bookings.FirstOrDefaultAsync(b => b.SlotId == 10 && b.UserId == 100);
        Assert.NotNull(booking);
    }

    [Fact]
    public async Task BookSlot_WithAlreadyBookedSlot_ReturnsBadRequest()
    {
        // Arrange
        var (context, unitOfWork) = CreateContextAndUnitOfWork();
        var controller = new BookingController(unitOfWork);
        SetupAuthenticatedUser(controller, "100");

        var turf = new Turf { Id = 1, Name = "Champion Field", Location = "Sector 5" };
        var slot = new Slot { Id = 10, TurfId = 1, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(1), IsBooked = true, Turf = turf };
        await context.Turfs.AddAsync(turf);
        await context.Slots.AddAsync(slot);
        await context.SaveChangesAsync();

        var dto = new CreateBookingDto { SlotId = 10 };

        // Act
        var response = await controller.BookSlot(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("Slot is already booked", apiResponse.Message);
    }

    [Fact]
    public async Task MyBookings_ReturnsOnlyLoggedUsersBookings()
    {
        // Arrange
        var (context, unitOfWork) = CreateContextAndUnitOfWork();
        var controller = new BookingController(unitOfWork);
        SetupAuthenticatedUser(controller, "100");

        var turf = new Turf { Id = 1, Name = "Champion Field", Location = "Sector 5" };
        var slot1 = new Slot { Id = 10, TurfId = 1, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(1), IsBooked = true, Turf = turf };
        var slot2 = new Slot { Id = 20, TurfId = 1, StartTime = DateTime.UtcNow.AddHours(1), EndTime = DateTime.UtcNow.AddHours(2), IsBooked = true, Turf = turf };

        var booking1 = new Booking { Id = 500, UserId = 100, SlotId = 10, Slot = slot1 };
        var booking2 = new Booking { Id = 600, UserId = 200, SlotId = 20, Slot = slot2 }; // different user

        await context.Turfs.AddAsync(turf);
        await context.Slots.AddRangeAsync(slot1, slot2);
        await context.Bookings.AddRangeAsync(booking1, booking2);
        await context.SaveChangesAsync();

        // Act
        var response = await controller.MyBookings();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(apiResponse.Success);

        Assert.NotNull(apiResponse.Data);
        var dataList = Assert.IsAssignableFrom<IEnumerable>(apiResponse.Data);
        var count = 0;
        foreach (var item in dataList)
        {
            Assert.NotNull(item);
            count++;
            var bookingIdProperty = item.GetType().GetProperty("bookingId");
            Assert.NotNull(bookingIdProperty);
            var bookingIdVal = (int?)bookingIdProperty.GetValue(item);
            Assert.Equal(500, bookingIdVal); // should only get booking for user 100
        }
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Cancel_FreesSlotAndDeletesBooking()
    {
        // Arrange
        var (context, unitOfWork) = CreateContextAndUnitOfWork();
        var controller = new BookingController(unitOfWork);
        SetupAuthenticatedUser(controller, "100");

        var turf = new Turf { Id = 1, Name = "Champion Field", Location = "Sector 5" };
        var slot = new Slot { Id = 10, TurfId = 1, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(1), IsBooked = true, Turf = turf };
        var booking = new Booking { Id = 500, UserId = 100, SlotId = 10, Slot = slot };

        await context.Turfs.AddAsync(turf);
        await context.Slots.AddAsync(slot);
        await context.Bookings.AddAsync(booking);
        await context.SaveChangesAsync();

        // Act
        var response = await controller.Cancel(500);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.False(slot.IsBooked); // should be freed

        var deletedBooking = await context.Bookings.FindAsync(500);
        Assert.Null(deletedBooking); // should be soft/hard deleted (controller calls unitOfWork.Bookings.Delete which calls context.Bookings.Remove)
    }
}
