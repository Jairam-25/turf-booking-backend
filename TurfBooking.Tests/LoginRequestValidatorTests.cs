using Application.DTOs;
using Application.Validators;
using FluentAssertions;
using Xunit;

namespace TurfBooking.Tests;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    // ──────────────────────────────────────────────
    // Valid input tests
    // ──────────────────────────────────────────────

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
        result.IsValid.Should().BeTrue();
    }

    // ──────────────────────────────────────────────
    // Invalid EmailOrPhone tests
    // ──────────────────────────────────────────────

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
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.EmailOrPhone));
    }

    // ──────────────────────────────────────────────
    // Invalid Password tests
    // ──────────────────────────────────────────────

    [Theory]
    [InlineData("")] // empty
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
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Password));
    }

    // ──────────────────────────────────────────────
    // Edge case — null-like inputs
    // ──────────────────────────────────────────────

    [Fact]
    public void Validator_WithWhitespaceOnlyPassword_Fails()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            EmailOrPhone = "test@example.com",
            Password = "   " // whitespace only
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_WithExactly6CharPassword_Passes()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            EmailOrPhone = "test@example.com",
            Password = "Ab1234" // exactly 6 chars — minimum length
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_WithPhoneNumberWithCountryCode_Passes()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            EmailOrPhone = "+919876543210",
            Password = "Password123"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
