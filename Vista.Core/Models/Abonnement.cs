using Vista.Core.Enums;
using Vista.Core.Models.Base;

namespace Vista.Core.Models;

/// <summary>
/// Taslak — Mandant'a ait abonelik planı.
/// </summary>
public class Abonnement : BasisEntity
{
    public Guid MandantId { get; set; }
    public Mandant Mandant { get; set; } = null!;

    public AbonnementPlan Plan { get; set; } = AbonnementPlan.Kostenlos;
    public string PlanName { get; set; } = string.Empty;
    public decimal Preis { get; set; }
    public DateTime StartDatum { get; set; }
    public DateTime? EndDatum { get; set; }
    public bool IstAktiv { get; set; } = true;

    // Navigation
    public ICollection<Rechnung> Rechnungen { get; set; } = new List<Rechnung>();
}
