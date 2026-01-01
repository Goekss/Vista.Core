using FluentValidation;
using Vista.Core.DTOs.Auth;

namespace Vista.Core.Validators.Auth;

public class VerifyCodeRequestDtoValidator : AbstractValidator<VerifyCodeRequestDto>
{
    public VerifyCodeRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(6)
            .Matches("^[0-9]{6}$");
    }
}
