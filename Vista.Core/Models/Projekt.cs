using Vista.Core.Models.Base;

namespace Vista.Core.Models;

public class Projekt : MandantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Beschreibung { get; set; } = string.Empty;
    public DateTime Startdatum { get; set; }
    public DateTime? Enddatum { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Prioritaet { get; set; } = string.Empty;
    public int AbschlussInProzent { get; set; }
    public bool IstAbgeschlossen { get; set; }

    public Guid KundeId { get; set; }
    public Kunde? Kunde { get; set; }

    public ICollection<Benutzer> Benutzer { get; set; } = new List<Benutzer>();
}
