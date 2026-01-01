using Microsoft.AspNetCore.Identity;

namespace Vista.Core.Models;

public class Benutzer : IdentityUser
{
    public string Vorname { get; set; } = string.Empty;
    public string Nachname { get; set; } = string.Empty;
    public string RufNummer { get; set; } = string.Empty;
    public string Abteilung { get; set; } = string.Empty;
    public string Rolle { get; set; } = string.Empty;
    public string Bild { get; set; } = string.Empty;
    public string Hinweise { get; set; } = string.Empty;

    // Kullanıcı hangi tenant/mandant'a aitse zorunlu alan
    public string MandantId { get; set; } = "00000000-0000-0000-0000-000000000000";

    public ICollection<Projekt> Projekte { get; set; } = new List<Projekt>();
}
