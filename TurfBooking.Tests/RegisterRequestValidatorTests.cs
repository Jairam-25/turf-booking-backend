using Application.DTOs;
using Application.Validators;
using FluentAssertions;
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
        result.IsValid.Should().BeTrue();
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
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Email));
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
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Password));
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
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.ConfirmPassword));
    }

    [Theory]
    [InlineData("")] // empty
    [InlineData("AB")] // too short (< 3 chars)
    public void Validator_WithInvalidName_Fails(string invalidName)
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Name = invalidName,
            Email = "john.doe@example.com",
            PhoneNumber = "9876543210",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Name));
    }

    [Theory]
    [InlineData("12345")] // too short
    [InlineData("12345678901")] // too long (11 digits)
    [InlineData("abcdefghij")] // letters, not digits
    [InlineData("")] // empty
    public void Validator_WithInvalidPhoneNumber_Fails(string invalidPhone)
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            PhoneNumber = invalidPhone,
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.PhoneNumber));
    }
}
