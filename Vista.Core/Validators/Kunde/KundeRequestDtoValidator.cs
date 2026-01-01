using FluentValidation;
using Vista.Core.DTOs.Kunde;

namespace Vista.Core.Validators.Kunde;

public class KundeRequestDtoValidator : AbstractValidator<KundeRequestDto>
{
    public KundeRequestDtoValidator()
    {
        RuleFor(x => x.Unternehmen).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Vorname).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Nachname).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.TelefonMobil).MaximumLength(50);
        RuleFor(x => x.TelefonHaus).MaximumLength(50);
        RuleFor(x => x.Adresse).MaximumLength(500);
        RuleFor(x => x.Website).MaximumLength(300);
        RuleFor(x => x.Logo).MaximumLength(500);
        RuleFor(x => x.Hinweise).MaximumLength(2000);
    }
}
