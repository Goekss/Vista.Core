using Vista.Core.Models.Base;

namespace Vista.Core.Models;

public class Kunde : MandantEntity
{
    public string Unternehmen { get; set; } = string.Empty;
    public string Vorname { get; set; } = string.Empty;
    public string Nachname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TelefonMobil { get; set; } = string.Empty;
    public string TelefonHaus { get; set; } = string.Empty;
    public string Adresse { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
    public string Hinweise { get; set; } = string.Empty;

    public ICollection<Filiale> Filialen { get; set; } = new List<Filiale>();
    public ICollection<Ansprechpartner> Ansprechpartner { get; set; } = new List<Ansprechpartner>();
    public ICollection<Projekt> Projekte { get; set; } = new List<Projekt>();
}
