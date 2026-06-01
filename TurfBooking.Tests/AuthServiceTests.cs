using Application.Common.Messages;
using Application.Common.Settings;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using FluentAssertions;
using Hangfire;
using Hangfire.Storage;
using Infrastructure.Services;
using Microsoft.Extensions.Options;
using Moq;

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

    // ──────────────────────────────────────────────
    // RegisterAsync Tests
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsFailure()
    {
        // Arrange
        var existingUser = new User { Email = "existing@example.com" };
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.Is<LoginRequestDto>(r => r.EmailOrPhone == "existing@example.com"), It.IsAny<CancellationToken>()))
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
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(AuthMessages.EmailAlreadyExists);
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_ReturnsSuccess()
    {
        // Arrange
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()))
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
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(AuthMessages.RegisterSuccess);
        _mockUserRepository.Verify(x => x.AddAsync(It.Is<User>(u => u.Email == "new@example.com"), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_PasswordIsHashedBeforeSaving()
    {
        // Arrange
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        User? capturedUser = null;
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => capturedUser = user);

        var registerRequest = new RegisterRequestDto
        {
            Name = "Hash Test User",
            Email = "hash@example.com",
            PhoneNumber = "9876543210",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Act
        await _authService.RegisterAsync(registerRequest);

        // Assert — password stored must NOT be the plain text
        capturedUser.Should().NotBeNull();
        capturedUser!.Password.Should().NotBe("Password123!",
            "the plain-text password must never be stored directly");

        // Assert — the stored hash must verify correctly against the original password
        BCrypt.Net.BCrypt.Verify("Password123!", capturedUser.Password)
            .Should().BeTrue("the stored password hash must match the original password");
    }

    // ──────────────────────────────────────────────
    // LoginAsync Tests
    // ──────────────────────────────────────────────

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

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var loginRequest = new LoginRequestDto
        {
            EmailOrPhone = "test@example.com",
            Password = "WrongPassword!"
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(AuthMessages.IncorrectEmailOrPassword);
        user.FailedLoginAttempts.Should().Be(1, "failed attempt should be incremented");
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var loginRequest = new LoginRequestDto
        {
            EmailOrPhone = "test@example.com",
            Password = password
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Test User");
        result.Value.Email.Should().Be("test@example.com");
        result.Value.Role.Should().Be("User");
        result.Value.Token.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();

        user.FailedLoginAttempts.Should().Be(0, "successful login resets failed attempts");
        user.IsLocked.Should().BeFalse();
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var loginRequest = new LoginRequestDto
        {
            EmailOrPhone = "locked@example.com",
            Password = "AnyPassword"
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(AuthMessages.LoginMaxAttempt);
    }
}
