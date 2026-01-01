using Vista.Core.Models.Base;

namespace Vista.Core.Models;

public class Ansprechpartner : MandantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Abteilung { get; set; } = string.Empty;

    public Guid KundeId { get; set; }
    public Kunde? Kunde { get; set; }

    public Guid? FilialeId { get; set; }
    public Filiale? Filiale { get; set; }
}
