using Vista.Core.Enums;

namespace Vista.Core.DTOs.Zahlung;

public class ZahlungResponseDto
{
    public Guid Id { get; set; }
    public Guid MandantId { get; set; }
    public Guid? RechnungId { get; set; }
    public decimal Betrag { get; set; }
    public DateTime ZahlungsDatum { get; set; }
    public ZahlungStatus Status { get; set; }
    public string? TransaktionId { get; set; }
    public string? IBAN { get; set; }
    public string? Hinweise { get; set; }
}

public class ZahlungRequestDto
{
    public Guid? RechnungId { get; set; }
    public decimal Betrag { get; set; }
    public string? IBAN { get; set; }
    public string? Hinweise { get; set; }
}
