using Application.Common.Messages;
using Application.Common.Result;
using Application.DTOs;
using Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using TurfBooking.API.Controllers;
using Xunit;

namespace TurfBooking.Tests;

public class AuthControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IOtpService> _mockOtpService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockOtpService = new Mock<IOtpService>();
        _controller = new AuthController(_mockMediator.Object, _mockOtpService.Object);
    }

    [Fact]
    public async Task Register_WithSuccessfulServiceCall_ReturnsOk()
    {
        // Arrange
        var request = new RegisterRequestDto { Email = "test@example.com", Password = "Password123!", ConfirmPassword = "Password123!", Name = "Test User", PhoneNumber = "1234567890" };
        var serviceResult = Result<string>.Success("User created successfully");
        _mockMediator
            .Setup(m => m.Send(It.IsAny<RegisterCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        // Act
        var response = await _controller.Register(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("User created successfully", apiResponse.Data);
    }

    [Fact]
    public async Task Register_WithFailedServiceCall_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequestDto { Email = "test@example.com", Password = "Password123!", ConfirmPassword = "Password123!", Name = "Test User", PhoneNumber = "1234567890" };
        var serviceResult = Result<string>.Failure("Email already exists");
        _mockMediator
            .Setup(m => m.Send(It.IsAny<RegisterCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        // Act
        var response = await _controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("Email already exists", apiResponse.Message);
    }

    [Fact]
    public async Task Login_WithSuccessfulServiceCall_ReturnsOk()
    {
        // Arrange
        var request = new LoginRequestDto { EmailOrPhone = "test@example.com", Password = "Password123!" };
        var loginResponse = new LoginResponseDto { Token = "access_token", RefreshToken = "refresh_token" };
        var serviceResult = Result<LoginResponseDto>.Success(loginResponse);
        _mockMediator
            .Setup(m => m.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        // Act
        var response = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<LoginResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("access_token", apiResponse!.Data!.Token);
    }

    [Fact]
    public async Task Login_WithFailedServiceCall_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequestDto { EmailOrPhone = "test@example.com", Password = "Password123!" };
        var serviceResult = Result<LoginResponseDto>.Failure(AuthMessages.InvalidCredentials);
        _mockMediator
            .Setup(m => m.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        // Act
        var response = await _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal(AuthMessages.InvalidCredentials, apiResponse.Message);
    }

    [Fact]
    public async Task RefreshToken_WithSuccessfulServiceCall_ReturnsOk()
    {
        // Arrange
        var token = "refresh_token";
        var loginResponse = new LoginResponseDto { Token = "new_access_token", RefreshToken = "new_refresh_token" };
        var serviceResult = Result<LoginResponseDto>.Success(loginResponse);
        _mockMediator
            .Setup(m => m.Send(It.IsAny<RefreshTokenCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        // Act
        var response = await _controller.RefreshToken(token);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<LoginResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("new_access_token", apiResponse!.Data!.Token);
    }

    [Fact]
    public async Task ForgotPassword_WithSuccessfulServiceCall_ReturnsOk()
    {
        // Arrange
        var request = new ForgotPasswordRequestDto { Email = "test@example.com" };
        var serviceResult = Result<string>.Success("Reset link sent");
        _mockMediator
            .Setup(m => m.Send(It.IsAny<ForgotPasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        // Act
        var response = await _controller.ForgotPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Reset link sent", apiResponse.Data);
    }

    [Fact]
    public async Task ResetPassword_WithSuccessfulServiceCall_ReturnsOk()
    {
        // Arrange
        var request = new ResetPasswordRequestDto { Token = "token", NewPassword = "NewPassword123!" };
        var serviceResult = Result<string>.Success("Password reset successfully");
        _mockMediator
            .Setup(m => m.Send(It.IsAny<ResetPasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        // Act
        var response = await _controller.ResetPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Password reset successfully", apiResponse.Data);
    }
}
