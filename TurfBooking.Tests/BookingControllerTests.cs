using Application.Common.Result;
using Application.DTOs;
using Application.Features.Booking.Commands;
using Application.Features.Booking.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TurfBooking.API.Controllers;
using Xunit;

namespace TurfBooking.Tests;

public class BookingControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly BookingController _controller;

    public BookingControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _controller = new BookingController(_mockMediator.Object);
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
        SetupAuthenticatedUser(_controller, "100");
        var dto = new CreateBookingDto { SlotId = 10 };
        var bookingData = new { bookingId = 1, slotId = 10, turfName = "Champion Field", location = "Sector 5" };
        
        _mockMediator
            .Setup(m => m.Send(It.Is<BookSlotCommand>(c => c.Request.SlotId == 10 && c.UserId == 100), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<object>.Success(bookingData));

        // Act
        var response = await _controller.BookSlot(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
    }

    [Fact]
    public async Task BookSlot_WithAlreadyBookedSlot_ReturnsBadRequest()
    {
        // Arrange
        SetupAuthenticatedUser(_controller, "100");
        var dto = new CreateBookingDto { SlotId = 10 };
        
        _mockMediator
            .Setup(m => m.Send(It.IsAny<BookSlotCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<object>.Failure("Slot is already booked"));

        // Act
        var response = await _controller.BookSlot(dto);

        // Assert
        var badRequestResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(400, badRequestResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("Slot is already booked", apiResponse.Message);
    }

    [Fact]
    public async Task MyBookings_ReturnsOnlyLoggedUsersBookings()
    {
        // Arrange
        SetupAuthenticatedUser(_controller, "100");
        var myBookingsList = new List<object> { new { bookingId = 1, turfName = "Champion Field" } };

        _mockMediator
            .Setup(m => m.Send(It.Is<GetMyBookingsQuery>(q => q.UserId == 100), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<object>.Success(myBookingsList));

        // Act
        var response = await _controller.MyBookings();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
    }

    [Fact]
    public async Task Cancel_FreesSlotAndDeletesBooking()
    {
        // Arrange
        SetupAuthenticatedUser(_controller, "100");

        _mockMediator
            .Setup(m => m.Send(It.Is<CancelBookingCommand>(c => c.BookingId == 500 && c.UserId == 100), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("Booking cancelled successfully"));

        // Act
        var response = await _controller.Cancel(500);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Booking cancelled successfully", apiResponse.Message);
    }
}
