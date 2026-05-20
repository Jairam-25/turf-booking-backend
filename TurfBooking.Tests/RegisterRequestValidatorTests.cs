using Application.DTOs;
using Application.Validators;
using Xunit;

namespace TurfBooking.Tests;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Validator_WithValidRequest_Passes()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "9876543210",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
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
    [InlineData("")]
    public void Validator_WithInvalidEmail_Fails(string invalidEmail)
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Name = "John Doe",
            Email = invalidEmail,
            PhoneNumber = "9876543210",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.Email));
    }

    [Theory]
    [InlineData("short")] // < 8 chars
    [InlineData("nouppercase123!")] // no uppercase
    [InlineData("NOLOWERCASE123!")] // no lowercase
    [InlineData("NoSpecialNumber")] // no number, no special char
    [InlineData("NoSpecial1")] // no special char
    public void Validator_WithWeakPassword_Fails(string weakPassword)
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "9876543210",
            Password = weakPassword,
            ConfirmPassword = weakPassword
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.Password));
    }

    [Fact]
    public void Validator_WithMismatchedConfirmPassword_Fails()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "9876543210",
            Password = "Password123!",
            ConfirmPassword = "Password321!"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.ConfirmPassword));
    }
}
