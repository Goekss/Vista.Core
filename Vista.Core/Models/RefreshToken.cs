using Vista.Core.Models.Base;

namespace Vista.Core.Models;

public class RefreshToken : BasisEntity
{
    public string Token { get; set; } = string.Empty;
    public string BenutzerId { get; set; } = string.Empty;
    public DateTime AblaufDatum { get; set; }
    public bool IstWiderrufen { get; set; }
    public string? ErsetztDurch { get; set; }

    public Benutzer Benutzer { get; set; } = null!;
}
