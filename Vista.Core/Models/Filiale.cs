using Vista.Core.Models.Base;

namespace Vista.Core.Models;

public class Filiale : MandantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Adresse { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;

    public Guid KundeId { get; set; }
    public Kunde? Kunde { get; set; }

    public ICollection<Ansprechpartner> Ansprechpartner { get; set; } = new List<Ansprechpartner>();
}
