using Vista.Core.Models.Base;

namespace Vista.Core.Models;

public class Ticket : MandantEntity
{
    public string Titel { get; set; } = string.Empty;
    public string Beschreibung { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Prioritaet { get; set; } = string.Empty;
    public string Kategorie { get; set; } = string.Empty;
    public DateTime? Faelligkeitsdatum { get; set; }

    public string? ZugewiesenAnId { get; set; }
    public Benutzer? ZugewiesenAn { get; set; }

    public Guid KundeId { get; set; }
    public Kunde? Kunde { get; set; }

    public Guid? ProjektId { get; set; }
    public Projekt? Projekt { get; set; }

    public ICollection<TicketNachricht> Nachrichten { get; set; } = new List<TicketNachricht>();
}
