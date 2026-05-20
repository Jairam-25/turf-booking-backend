using Application.Common.Messages;
using Application.Common.Settings;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Hangfire;
using Hangfire.Storage;
using Infrastructure.Services;
using Microsoft.Extensions.Options;
using Moq;
using Persistence.Interfaces;
using System;
using System.Threading.Tasks;
using Xunit;

namespace TurfBooking.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IOptions<JwtSettings>> _mockJwtSettings;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockEmailService = new Mock<IEmailService>();
        _mockJwtSettings = new Mock<IOptions<JwtSettings>>();

        var jwtSettings = new JwtSettings
        {
            Key = "super_secret_key_that_is_long_enough_to_be_secure_12345!",
            Issuer = "TurfBooking",
            Audience = "TurfBookingUsers"
        };
        _mockJwtSettings.Setup(x => x.Value).Returns(jwtSettings);

        _authService = new AuthService(
            _mockUserRepository.Object,
            _mockUnitOfWork.Object,
            _mockJwtSettings.Object,
            _mockEmailService.Object
        );

        // Mock Hangfire JobStorage and IBackgroundJobClient to prevent static BackgroundJob.Enqueue from throwing
        var mockStorage = new Mock<JobStorage>();
        var mockConnection = new Mock<IStorageConnection>();
        mockStorage.Setup(x => x.GetConnection()).Returns(mockConnection.Object);
        JobStorage.Current = mockStorage.Object;
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsFailure()
    {
        // Arrange
        var existingUser = new User { Email = "existing@example.com" };
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.Is<LoginRequestDto>(r => r.EmailOrPhone == "existing@example.com")))
            .ReturnsAsync(existingUser);

        var registerRequest = new RegisterRequestDto
        {
            Name = "New User",
            Email = "existing@example.com",
            PhoneNumber = "9876543210",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Act
        var result = await _authService.RegisterAsync(registerRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(AuthMessages.EmailAlreadyExists, result.Error);
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_ReturnsSuccess()
    {
        // Arrange
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync((User?)null);

        var registerRequest = new RegisterRequestDto
        {
            Name = "New User",
            Email = "new@example.com",
            PhoneNumber = "9876543210",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Act
        var result = await _authService.RegisterAsync(registerRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AuthMessages.RegisterSuccess, result.Value);
        _mockUserRepository.Verify(x => x.AddAsync(It.Is<User>(u => u.Email == "new@example.com")), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsFailure()
    {
        // Arrange
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!");
        var user = new User
        {
            Email = "test@example.com",
            Password = hashedPassword,
            FailedLoginAttempts = 0,
            IsLocked = false
        };

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(user);

        var loginRequest = new LoginRequestDto
        {
            EmailOrPhone = "test@example.com",
            Password = "WrongPassword!"
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(AuthMessages.IncorrectEmailOrPassword, result.Error);
        Assert.Equal(1, user.FailedLoginAttempts); // Failed attempt incremented
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var password = "CorrectPassword123!";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            PhoneNumber = "9876543210",
            Password = hashedPassword,
            Role = "User",
            FailedLoginAttempts = 2,
            IsLocked = false
        };

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(user);

        var loginRequest = new LoginRequestDto
        {
            EmailOrPhone = "test@example.com",
            Password = password
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Test User", result.Value.Name);
        Assert.Equal("test@example.com", result.Value.Email);
        Assert.Equal("User", result.Value.Role);
        Assert.NotEmpty(result.Value.Token);
        Assert.NotEmpty(result.Value.RefreshToken);

        Assert.Equal(0, user.FailedLoginAttempts); // Resets failed login attempts
        Assert.False(user.IsLocked);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithLockedUser_ReturnsFailure()
    {
        // Arrange
        var user = new User
        {
            Email = "locked@example.com",
            IsLocked = true,
            LockoutEnd = DateTime.UtcNow.AddMinutes(5)
        };

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(user);

        var loginRequest = new LoginRequestDto
        {
            EmailOrPhone = "locked@example.com",
            Password = "AnyPassword"
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(AuthMessages.LoginMaxAttempt, result.Error);
    }
}
