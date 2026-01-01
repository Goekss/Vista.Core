using Vista.Core.Models.Base;

namespace Vista.Core.Models;

public class TicketNachricht : MandantEntity
{
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public string AbsenderId { get; set; } = string.Empty;
    public Benutzer? Absender { get; set; }

    public string Inhalt { get; set; } = string.Empty;
    public bool IstInternNotiz { get; set; }
    public DateTime GeschicktAm { get; set; } = DateTime.UtcNow;
}
