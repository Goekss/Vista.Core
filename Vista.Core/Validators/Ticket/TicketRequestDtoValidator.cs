using FluentValidation;
using Vista.Core.DTOs.Ticket;

namespace Vista.Core.Validators.Ticket;

public class TicketRequestDtoValidator : AbstractValidator<TicketRequestDto>
{
    public TicketRequestDtoValidator()
    {
        RuleFor(x => x.Titel).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Beschreibung).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Status)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.Status));
        RuleFor(x => x.Prioritaet).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Kategorie).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ProjektId)
            .NotEqual(Guid.Empty)
            .When(x => x.ProjektId.HasValue);
        RuleFor(x => x.ZugewiesenAnId)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.ZugewiesenAnId));
    }
}
