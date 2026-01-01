namespace Vista.Core.Models.Base
{
    public class BasisEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime ErstelltAm { get; set; } = DateTime.UtcNow;
        public DateTime? AktualisiertAm { get; set; }
        public bool IstGeloescht { get; set; }
    }
}
