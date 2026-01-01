using Vista.Core.Enums;
using Vista.Core.Models.Base;

namespace Vista.Core.Models;

/// <summary>
/// Taslak — Fatura kaydı.
/// </summary>
public class Rechnung : BasisEntity
{
    public Guid MandantId { get; set; }
    public Mandant Mandant { get; set; } = null!;

    public Guid AbonnementId { get; set; }
    public Abonnement Abonnement { get; set; } = null!;

    public string Nummer { get; set; } = string.Empty;
    public decimal Betrag { get; set; }
    public DateTime RechnungsDatum { get; set; }
    public DateTime? FaelligkeitsDatum { get; set; }
    public RechnungStatus Status { get; set; } = RechnungStatus.Entwurf;
    public string? PdfPfad { get; set; }

    // Navigation
    public ICollection<Zahlung> Zahlungen { get; set; } = new List<Zahlung>();
}
