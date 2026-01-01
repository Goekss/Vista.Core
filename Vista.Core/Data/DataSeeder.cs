using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vista.Core.Models;

namespace Vista.Core.Data;

public static class DataSeeder
{
    public static async Task SeedRolesAndUsersAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<Benutzer>>();
        var env = serviceProvider.GetRequiredService<IWebHostEnvironment>();
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

        var defaultMandantId = await dbContext.Mandanten
            .AsNoTracking()
            .Select(m => m.Id)
            .FirstOrDefaultAsync();

        string[] rollen = ["SuperAdmin", "Admin", "Manager", "Standard", "NurLesen"];

        foreach (var rolle in rollen)
        {
            if (!await roleManager.RoleExistsAsync(rolle))
                await roleManager.CreateAsync(new IdentityRole(rolle));
        }

        // Demo admin: Development'ta her zaman, Production'da sadece SEED_ADMIN=true ise.
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var seedAdminInProd = config.GetValue<bool>("SEED_ADMIN");
        if (!env.IsDevelopment() && !seedAdminInProd)
        {
            return;
        }

        const string adminEmail = "admin@vista.local";
        var adminPassword = config["SEED_ADMIN_PASSWORD"] ?? "Test123!";
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

        var resolvedMandantId = defaultMandantId == Guid.Empty
            ? "00000000-0000-0000-0000-000000000000"
            : defaultMandantId.ToString();

        if (existingAdmin is null)
        {
            var admin = new Benutzer
            {
                UserName = adminEmail,
                Email = adminEmail,
                Vorname = "System",
                Nachname = "Admin",
                MandantId = resolvedMandantId,
                EmailConfirmed = true,
                LockoutEnabled = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "SuperAdmin");
        }
        else
        {
            if (!Guid.TryParse(existingAdmin.MandantId, out _))
            {
                existingAdmin.MandantId = resolvedMandantId;
                await userManager.UpdateAsync(existingAdmin);
            }

            await userManager.SetLockoutEnabledAsync(existingAdmin, true);
            await userManager.SetLockoutEndDateAsync(existingAdmin, null);
            await userManager.ResetAccessFailedCountAsync(existingAdmin);
            var token = await userManager.GeneratePasswordResetTokenAsync(existingAdmin);
            await userManager.ResetPasswordAsync(existingAdmin, token, adminPassword);
        }

        if (defaultMandantId != Guid.Empty)
        {
            var allUsers = await userManager.Users.ToListAsync();
            var usersWithoutMandant = allUsers
                .Where(u => !Guid.TryParse(u.MandantId, out _))
                .ToList();

            foreach (var user in usersWithoutMandant)
            {
                user.MandantId = defaultMandantId.ToString();
            }

            if (usersWithoutMandant.Count > 0)
            {
                await dbContext.SaveChangesAsync();
            }
        }
    }

    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        if (await dbContext.Mandanten.AnyAsync(cancellationToken))
        {
            return;
        }

        var mandant = new Mandant
        {
            Name = "Demo Mandant",
            Domain = "demo.vista.local",
            IstAktiv = true
        };

        var kunde = new Kunde
        {
            MandantId = mandant.Id,
            Unternehmen = "X Bank",
            Vorname = "Ali",
            Nachname = "Yilmaz",
            Email = "info@xbank.local",
            TelefonMobil = "+49 170 000 0000",
            TelefonHaus = "+49 30 000 000",
            Adresse = "Berlin",
            Website = "https://xbank.local",
            Hinweise = "Demo musteri kaydi"
        };

        var filiale = new Filiale
        {
            MandantId = mandant.Id,
            Kunde = kunde,
            Name = "X Bank Berlin Subesi",
            Adresse = "Berlin Mitte",
            Telefon = "+49 30 111 111"
        };

        var ansprechpartner = new Ansprechpartner
        {
            MandantId = mandant.Id,
            Kunde = kunde,
            Filiale = filiale,
            Name = "Ayse Kara",
            Telefon = "+49 170 111 1111",
            Email = "ayse.kara@xbank.local",
            Abteilung = "Destek"
        };

        var projekt = new Projekt
        {
            MandantId = mandant.Id,
            Kunde = kunde,
            Name = "ATM Ariza Takip",
            Beschreibung = "Banka ATM arizalarinin izlenmesi",
            Startdatum = DateTime.UtcNow.Date,
            Status = "InBearbeitung",
            Prioritaet = "Hoch",
            AbschlussInProzent = 15,
            IstAbgeschlossen = false
        };

        var ticket = new Ticket
        {
            MandantId = mandant.Id,
            Kunde = kunde,
            Projekt = projekt,
            Titel = "ATM ekrani siyah kaldi",
            Beschreibung = "Musteri ATM ekraninin calismadigini bildirdi.",
            Status = "Offen",
            Prioritaet = "Hoch",
            Kategorie = "ATM",
            Faelligkeitsdatum = DateTime.UtcNow.AddDays(2)
        };

        var bericht = new Bericht
        {
            MandantId = mandant.Id,
            Titel = "Haftalik Servis Raporu",
            DateiPfad = "/Storage/berichte/demo.pdf",
            DateiTyp = "application/pdf",
            Version = "v1"
        };

        var chatRaum = new ChatRaum
        {
            MandantId = mandant.Id,
            Name = "Ticket-ATM-Chat",
            Projekt = projekt,
            Ticket = ticket
        };

        dbContext.Mandanten.Add(mandant);
        dbContext.Kunden.Add(kunde);
        dbContext.Filialen.Add(filiale);
        dbContext.Ansprechpartner.Add(ansprechpartner);
        dbContext.Projekte.Add(projekt);
        dbContext.Tickets.Add(ticket);
        dbContext.Berichte.Add(bericht);
        dbContext.ChatRaeume.Add(chatRaum);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
