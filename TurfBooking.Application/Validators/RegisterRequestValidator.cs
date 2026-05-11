using Application.DTOs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validators
{
    public class RegisterRequestValidator
    : AbstractValidator<RegisterRequestDto>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MinimumLength(3);

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Matches("[A-Z]")
                .WithMessage("Password must contain uppercase letter")
                .Matches("[a-z]")
                .WithMessage("Password must contain lowercase letter")
                .Matches("[0-9]")
                .WithMessage("Password must contain number")
                .Matches("[^a-zA-Z0-9]")
                .WithMessage("Password must contain special character");
        }
    }
}
