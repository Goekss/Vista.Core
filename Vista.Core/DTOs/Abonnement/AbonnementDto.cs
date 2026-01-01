using Vista.Core.Enums;

namespace Vista.Core.DTOs.Abonnement;

public class AbonnementResponseDto
{
    public Guid Id { get; set; }
    public Guid MandantId { get; set; }
    public AbonnementPlan Plan { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal Preis { get; set; }
    public DateTime StartDatum { get; set; }
    public DateTime? EndDatum { get; set; }
    public bool IstAktiv { get; set; }
}

public class AbonnementRequestDto
{
    public AbonnementPlan Plan { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal Preis { get; set; }
    public DateTime StartDatum { get; set; }
    public DateTime? EndDatum { get; set; }
}
