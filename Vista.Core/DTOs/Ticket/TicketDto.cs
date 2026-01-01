namespace Vista.Core.DTOs.Ticket;

public class TicketRequestDto
{
    public string Titel { get; set; } = string.Empty;
    public string Beschreibung { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Prioritaet { get; set; } = string.Empty;
    public string Kategorie { get; set; } = string.Empty;
    public DateTime? Faelligkeitsdatum { get; set; }
    public string? ZugewiesenAnId { get; set; }
    public Guid KundeId { get; set; }
    public Guid? ProjektId { get; set; }
}

public class TicketResponseDto : TicketRequestDto
{
    public Guid Id { get; set; }
    public Guid MandantId { get; set; }
}
