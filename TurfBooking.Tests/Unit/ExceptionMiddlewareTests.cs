using Application.Common.Result;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using TurfBooking.API.Middlewares;

namespace TurfBooking.Tests;

public class ExceptionMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionMiddleware>> _mockLogger = new();
    private readonly Mock<IWebHostEnvironment> _mockEnv = new();

    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNext()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ExceptionMiddleware(next, _mockLogger.Object, _mockEnv.Object);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        RequestDelegate next = (ctx) => throw new Exception("Unexpected error");
        _mockEnv.Setup(e => e.EnvironmentName).Returns("Production");

        var middleware = new ExceptionMiddleware(next, _mockLogger.Object, _mockEnv.Object);
        var context = new DefaultHttpContext();
        
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);

        responseStream.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(responseStream);
        var responseBody = await reader.ReadToEndAsync();
        
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Equal("An unexpected error occurred. Please try again later.", apiResponse.Message);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationExceptionThrown_ReturnsBadRequest()
    {
        // Arrange
        var validationErrors = new List<ValidationFailure>
        {
            new ValidationFailure("Email", "Email is required")
        };
        RequestDelegate next = (ctx) => throw new ValidationException("Validation failed.", validationErrors);

        var middleware = new ExceptionMiddleware(next, _mockLogger.Object, _mockEnv.Object);
        var context = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);

        responseStream.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(responseStream);
        var responseBody = await reader.ReadToEndAsync();
        
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Equal("Validation failed.", apiResponse.Message);
        Assert.NotNull(apiResponse.Errors);
        Assert.Contains("Email is required", apiResponse.Errors);
    }
}
