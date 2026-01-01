namespace Vista.Core.DTOs.Projekt;

public class ProjektRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Beschreibung { get; set; } = string.Empty;
    public DateTime Startdatum { get; set; }
    public DateTime? Enddatum { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Prioritaet { get; set; } = string.Empty;
    public int AbschlussInProzent { get; set; }
    public bool IstAbgeschlossen { get; set; }
    public Guid KundeId { get; set; }
    public List<string> BenutzerIds { get; set; } = new();
}

public class ProjektResponseDto : ProjektRequestDto
{
    public Guid Id { get; set; }
    public Guid MandantId { get; set; }
}
