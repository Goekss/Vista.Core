using FluentValidation;
using Vista.Core.DTOs.Auth;

namespace Vista.Core.Validators.Auth;

public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Passwort)
            .NotEmpty()
            .MinimumLength(6)
            .MaximumLength(128);
    }
}
