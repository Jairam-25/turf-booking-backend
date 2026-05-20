using Application.DTOs;
using Application.Validators;
using Xunit;

namespace TurfBooking.Tests;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name+tag@domain.co.uk")]
    [InlineData("9876543210")]
    [InlineData("1234567890")]
    public void Validator_WithValidEmailOrPhone_Passes(string validEmailOrPhone)
    {
        // Arrange
        var request = new LoginRequestDto
        {
            EmailOrPhone = validEmailOrPhone,
            Password = "Password123"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@test.com")]
    [InlineData("12345")] // too short for phone
    [InlineData("12345678901")] // too long for phone
    [InlineData("")]
    [InlineData("   ")]
    public void Validator_WithInvalidEmailOrPhone_Fails(string invalidEmailOrPhone)
    {
        // Arrange
        var request = new LoginRequestDto
        {
            EmailOrPhone = invalidEmailOrPhone,
            Password = "Password123"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.EmailOrPhone));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")] // < 6 chars
    public void Validator_WithInvalidPassword_Fails(string invalidPassword)
    {
        // Arrange
        var request = new LoginRequestDto
        {
            EmailOrPhone = "test@example.com",
            Password = invalidPassword
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.Password));
    }
}
