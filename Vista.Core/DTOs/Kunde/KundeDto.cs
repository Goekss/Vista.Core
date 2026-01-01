namespace Vista.Core.DTOs.Kunde;

public class KundeRequestDto
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
}

public class KundeResponseDto : KundeRequestDto
{
    public Guid Id { get; set; }
    public Guid MandantId { get; set; }
}
