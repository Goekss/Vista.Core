using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vista.Core.Migrations
{
    /// <inheritdoc />
    public partial class TenantIsolationsIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Zahlungen_MandantId",
                table: "Zahlungen");

            migrationBuilder.DropIndex(
                name: "IX_Rechnungen_MandantId",
                table: "Rechnungen");

            migrationBuilder.DropIndex(
                name: "IX_Abonnements_MandantId",
                table: "Abonnements");

            migrationBuilder.CreateIndex(
                name: "IX_Zahlung_Mandant_Geloescht",
                table: "Zahlungen",
                columns: new[] { "MandantId", "IstGeloescht" });

            migrationBuilder.CreateIndex(
                name: "IX_Ticket_Mandant_Geloescht",
                table: "Tickets",
                columns: new[] { "MandantId", "IstGeloescht" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketNachricht_Mandant_Geloescht",
                table: "TicketNachrichten",
                columns: new[] { "MandantId", "IstGeloescht" });

            migrationBuilder.CreateIndex(
                name: "IX_Rechnung_Mandant_Geloescht",
                table: "Rechnungen",
                columns: new[] { "MandantId", "IstGeloescht" });

            migrationBuilder.CreateIndex(
                name: "IX_Projekt_Mandant_Geloescht",
                table: "Projekte",
                columns: new[] { "MandantId", "IstGeloescht" });

            migrationBuilder.CreateIndex(
                name: "IX_Kunde_Mandant_Geloescht",
                table: "Kunden",
                columns: new[] { "MandantId", "IstGeloescht" });

            migrationBuilder.CreateIndex(
                name: "IX_Filiale_Mandant_Geloescht",
                table: "Filialen",
                columns: new[] { "MandantId", "IstGeloescht" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatRaum_Mandant_Geloescht",
                table: "ChatRaeume",
                columns: new[] { "MandantId", "IstGeloescht" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatNachricht_Mandant_Geloescht",
                table: "ChatNachrichten",
                columns: new[] { "MandantId", "IstGeloescht" });

            migrationBuilder.CreateIndex(
                name: "IX_Bericht_Mandant_Geloescht",
                table: "Berichte",
                columns: new[] { "MandantId", "IstGeloescht" });

            migrationBuilder.CreateIndex(
                name: "IX_Ansprechpartner_Mandant_Geloescht",
                table: "Ansprechpartner",
                columns: new[] { "MandantId", "IstGeloescht" });

            migrationBuilder.CreateIndex(
                name: "IX_Abonnement_Mandant_Geloescht",
                table: "Abonnements",
                columns: new[] { "MandantId", "IstGeloescht" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Zahlung_Mandant_Geloescht",
                table: "Zahlungen");

            migrationBuilder.DropIndex(
                name: "IX_Ticket_Mandant_Geloescht",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_TicketNachricht_Mandant_Geloescht",
                table: "TicketNachrichten");

            migrationBuilder.DropIndex(
                name: "IX_Rechnung_Mandant_Geloescht",
                table: "Rechnungen");

            migrationBuilder.DropIndex(
                name: "IX_Projekt_Mandant_Geloescht",
                table: "Projekte");

            migrationBuilder.DropIndex(
                name: "IX_Kunde_Mandant_Geloescht",
                table: "Kunden");

            migrationBuilder.DropIndex(
                name: "IX_Filiale_Mandant_Geloescht",
                table: "Filialen");

            migrationBuilder.DropIndex(
                name: "IX_ChatRaum_Mandant_Geloescht",
                table: "ChatRaeume");

            migrationBuilder.DropIndex(
                name: "IX_ChatNachricht_Mandant_Geloescht",
                table: "ChatNachrichten");

            migrationBuilder.DropIndex(
                name: "IX_Bericht_Mandant_Geloescht",
                table: "Berichte");

            migrationBuilder.DropIndex(
                name: "IX_Ansprechpartner_Mandant_Geloescht",
                table: "Ansprechpartner");

            migrationBuilder.DropIndex(
                name: "IX_Abonnement_Mandant_Geloescht",
                table: "Abonnements");

            migrationBuilder.CreateIndex(
                name: "IX_Zahlungen_MandantId",
                table: "Zahlungen",
                column: "MandantId");

            migrationBuilder.CreateIndex(
                name: "IX_Rechnungen_MandantId",
                table: "Rechnungen",
                column: "MandantId");

            migrationBuilder.CreateIndex(
                name: "IX_Abonnements_MandantId",
                table: "Abonnements",
                column: "MandantId");
        }
    }
}
