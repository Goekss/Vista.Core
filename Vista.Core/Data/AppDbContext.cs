using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Vista.Core.Models;
using Vista.Core.Models.Base;

namespace Vista.Core.Data;

public class AppDbContext : IdentityDbContext<Benutzer>
{
    private readonly Guid? _currentMandantId;

    public Guid? CurrentMandantId => _currentMandantId;

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        var mandantClaim = httpContextAccessor?.HttpContext?.User?.FindFirst("MandantId")?.Value;

        if (Guid.TryParse(mandantClaim, out var mandantId))
        {
            _currentMandantId = mandantId;
        }
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, Guid? currentMandantId)
        : base(options)
    {
        _currentMandantId = currentMandantId;
    }

    public DbSet<Mandant> Mandanten => Set<Mandant>();
    public DbSet<Kunde> Kunden => Set<Kunde>();
    public DbSet<Filiale> Filialen => Set<Filiale>();
    public DbSet<Ansprechpartner> Ansprechpartner => Set<Ansprechpartner>();
    public DbSet<Projekt> Projekte => Set<Projekt>();
    public DbSet<Bericht> Berichte => Set<Bericht>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketNachricht> TicketNachrichten => Set<TicketNachricht>();
    public DbSet<ChatRaum> ChatRaeume => Set<ChatRaum>();
    public DbSet<ChatNachricht> ChatNachrichten => Set<ChatNachricht>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Taslak — Ödeme & Abonelik
    public DbSet<Abonnement> Abonnements => Set<Abonnement>();
    public DbSet<Zahlung> Zahlungen => Set<Zahlung>();
    public DbSet<Rechnung> Rechnungen => Set<Rechnung>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(MandantEntity).IsAssignableFrom(t.ClrType)))
        {
            var method = typeof(AppDbContext)
                .GetMethod(nameof(SetMandantFilter), BindingFlags.NonPublic | BindingFlags.Instance)?
                .MakeGenericMethod(entityType.ClrType);

            method?.Invoke(this, new object[] { modelBuilder });
        }
    }

    // TR: Her sorguda kiracı filtresini otomatik uygularız, böylece başka şirketin verisi asla gelmez.
    // DE: Automatischer Mandantenfilter bei jeder Abfrage, um Datenlecks zwischen Mandanten zu verhindern.
    private void SetMandantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : MandantEntity
    {
        // TR: MandantId eşleşmeyen veya null olan isteklerde veriyi gizle.
        // DE: Datensätze ausblenden, wenn MandantId nicht übereinstimmt oder null ist.
        modelBuilder.Entity<TEntity>().HasQueryFilter(
            e => _currentMandantId != null && e.MandantId == _currentMandantId);

        // TR: 500-1000 eşzamanlı istekte sorgu hızını korumak için bileşik indeks.
        // DE: Zusammengesetzter Index für hohe Performance bei gleichzeitigen Abfragen.
        modelBuilder.Entity<TEntity>()
            .HasIndex(e => new { e.MandantId, e.IstGeloescht })
            .HasDatabaseName($"IX_{typeof(TEntity).Name}_Mandant_Geloescht");
    }
}
