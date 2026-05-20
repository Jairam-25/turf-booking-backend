using Application.Common.Result;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TurfBooking.API.Controllers;
using Xunit;

namespace TurfBooking.Tests;

public class SlotControllerTests
{
    private (ApplicationDbContext Context, UnitOfWork UnitOfWork) CreateContextAndUnitOfWork()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var unitOfWork = new UnitOfWork(context);

        return (context, unitOfWork);
    }

    [Fact]
    public async Task GetAvailableSlots_ReturnsOnlyAvailableSlotsForSpecificTurf()
    {
        // Arrange
        var (context, unitOfWork) = CreateContextAndUnitOfWork();
        var controller = new SlotController(unitOfWork);

        var turf1 = new Turf { Id = 10, Name = "Turf A", Location = "Loc A" };
        var turf2 = new Turf { Id = 20, Name = "Turf B", Location = "Loc B" };
        await context.Turfs.AddRangeAsync(turf1, turf2);

        var slot1 = new Slot { Id = 1, TurfId = 10, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(1), IsBooked = false };
        var slot2 = new Slot { Id = 2, TurfId = 10, StartTime = DateTime.UtcNow.AddHours(1), EndTime = DateTime.UtcNow.AddHours(2), IsBooked = true }; // booked
        var slot3 = new Slot { Id = 3, TurfId = 20, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(1), IsBooked = false }; // different turf

        await context.Slots.AddRangeAsync(slot1, slot2, slot3);
        await context.SaveChangesAsync();

        // Act
        var response = await controller.GetAvailableSlots(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(apiResponse.Success);

        var dataList = Assert.IsAssignableFrom<IEnumerable>(apiResponse.Data);
        var count = 0;
        foreach (var item in dataList)
        {
            count++;
            // Use reflection or dynamic to assert properties of the anonymous type
            var idProperty = item.GetType().GetProperty("slotId");
            var slotIdVal = (int)idProperty.GetValue(item);
            Assert.Equal(1, slotIdVal); // should only return slot 1
        }
        Assert.Equal(1, count);
    }
}
