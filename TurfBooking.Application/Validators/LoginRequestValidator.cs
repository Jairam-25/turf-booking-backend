using Application.DTOs;
using FluentValidation;
using System.Text.RegularExpressions;

namespace Application.Validators;

public class LoginRequestValidator
    : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.EmailOrPhone)
            .NotEmpty()
            .Must(BeValidEmailOrPhone)
            .WithMessage(
                "Enter valid email or phone number");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6);
    }

    private bool BeValidEmailOrPhone(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Email validation
        var isEmail =
            Regex.IsMatch(
                value,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        // Phone validation
        var isPhone =
            Regex.IsMatch(
                value,
                @"^[0-9]{10}$");

        return isEmail || isPhone;
    }
}