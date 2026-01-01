using FluentValidation;
using Vista.Core.DTOs.Projekt;

namespace Vista.Core.Validators.Projekt;

public class ProjektRequestDtoValidator : AbstractValidator<ProjektRequestDto>
{
    public ProjektRequestDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Beschreibung).MaximumLength(4000);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Prioritaet).NotEmpty().MaximumLength(50);
        RuleFor(x => x.KundeId).NotEqual(Guid.Empty);
        RuleFor(x => x.AbschlussInProzent).InclusiveBetween(0, 100);
        RuleFor(x => x.Enddatum)
            .GreaterThanOrEqualTo(x => x.Startdatum)
            .When(x => x.Enddatum.HasValue);
    }
}
