using Application.DTOs;
using FluentValidation;
using System.Text.RegularExpressions;

namespace Application.Validators;

public class RegisterRequestValidator
    : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^(\+91)?[0-9]{10}$")
            .WithMessage(
                "Enter valid phone number");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)

            .Matches("[A-Z]")
            .WithMessage(
                "Password must contain uppercase letter")

            .Matches("[a-z]")
            .WithMessage(
                "Password must contain lowercase letter")

            .Matches("[0-9]")
            .WithMessage(
                "Password must contain number")

            .Matches("[^a-zA-Z0-9]")
            .WithMessage(
                "Password must contain special character");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .Equal(x => x.Password)
            .WithMessage(
                "Passwords do not match");
    }
}