using Vista.Core.Models.Base;

namespace Vista.Core.Models;

public class Mandant : BasisEntity
{
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public bool IstAktiv { get; set; } = true;
}
