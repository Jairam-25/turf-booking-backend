using Application.Common.Result;
using Application.DTOs;
using Application.Features.Turf.Queries;
using Application.Features.Turf.Commands;
using Application.Model;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TurfBooking.API.Controllers;
using Xunit;

namespace TurfBooking.Tests;

public class TurfControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly TurfController _controller;

    public TurfControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _controller = new TurfController(_mockMediator.Object);
    }

    [Fact]
    public async Task Get_ReturnsOkWithPagedResult()
    {
        // Arrange
        var query = new TurfQueryParameters { Page = 1, PageSize = 10 };
        var pagedResult = new PagedResult<TurfResponseDto>
        {
            Items = new List<TurfResponseDto> { new() { Id = 1, Name = "Golden Turf", Location = "Downtown", PricePerHour = 100 } },
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetAllTurfsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var response = await _controller.Get(query);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<PagedResult<TurfResponseDto>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(1, apiResponse!.Data!.TotalCount);
        Assert.Equal("Golden Turf", apiResponse.Data.Items.First().Name);
    }

    [Fact]
    public async Task Create_ReturnsOkWithTurfResponse()
    {
        // Arrange
        var dto = new CreateTurfDto { Name = "New Turf", Location = "Uptown", PricePerHour = 150 };
        var responseDto = new TurfResponseDto { Id = 1, Name = "New Turf", Location = "Uptown", PricePerHour = 150 };
        _mockMediator.Setup(x => x.Send(It.IsAny<CreateTurfCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(responseDto);

        // Act
        var response = await _controller.Create(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<TurfResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("New Turf", apiResponse!.Data!.Name);
    }

    [Fact]
    public async Task Delete_WhenTurfExists_ReturnsOk()
    {
        // Arrange
        _mockMediator.Setup(x => x.Send(It.IsAny<DeleteTurfCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var response = await _controller.Delete(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Turf deleted successfully", apiResponse.Message);
    }

    [Fact]
    public async Task Delete_WhenTurfDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        _mockMediator.Setup(x => x.Send(It.IsAny<DeleteTurfCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var response = await _controller.Delete(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("Turf not found", apiResponse.Message);
    }
}
