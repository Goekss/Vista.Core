using Vista.Core.Enums;
using Vista.Core.Models.Base;

namespace Vista.Core.Models;

/// <summary>
/// Taslak — Ödeme kaydı. Gerçek ödeme entegrasyonu (Stripe/Iyzico) ileride eklenecek.
/// </summary>
public class Zahlung : BasisEntity
{
    public Guid MandantId { get; set; }
    public Mandant Mandant { get; set; } = null!;

    public Guid? RechnungId { get; set; }
    public Rechnung? Rechnung { get; set; }

    public decimal Betrag { get; set; }
    public DateTime ZahlungsDatum { get; set; }
    public ZahlungStatus Status { get; set; } = ZahlungStatus.Ausstehend;
    public string? TransaktionId { get; set; }
    public string? IBAN { get; set; }
    public string? Hinweise { get; set; }
}
