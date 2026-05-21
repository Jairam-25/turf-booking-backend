using Application.Common.Result;
using Application.DTOs;
using Application.Features.Slot.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TurfBooking.API.Controllers;
using Xunit;

namespace TurfBooking.Tests;

public class SlotControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly SlotController _controller;

    public SlotControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _controller = new SlotController(_mockMediator.Object);
    }

    [Fact]
    public async Task GetAvailableSlots_ReturnsOnlyAvailableSlotsForSpecificTurf()
    {
        // Arrange
        var slotsList = new List<SlotResponseDto>
        {
            new() { SlotId = 1, TurfId = 10, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(1) }
        };

        _mockMediator
            .Setup(m => m.Send(It.Is<GetAvailableSlotsQuery>(q => q.TurfId == 10), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<SlotResponseDto>>.Success(slotsList));

        // Act
        var response = await _controller.GetAvailableSlots(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        var dataList = Assert.IsAssignableFrom<IEnumerable<SlotResponseDto>>(apiResponse.Data);
        Assert.Single(dataList);
    }
}
