using Vista.Core.Models.Base;

namespace Vista.Core.Models;

public class Bericht : MandantEntity
{
    public string Titel { get; set; } = string.Empty;
    public string DateiPfad { get; set; } = string.Empty;
    public string DateiTyp { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime HochgeladenAm { get; set; } = DateTime.UtcNow;
    public DateTime? BearbeitetAm { get; set; }
}
