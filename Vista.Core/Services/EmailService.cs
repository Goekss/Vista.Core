using MailKit.Net.Smtp;
using MimeKit;

namespace Vista.Core.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendVerificationCodeAsync(string empfaengerEmail, string code)
    {
        var betreff = "Vista — Ihr Anmeldecode";
        var inhalt = $@"
            <html>
            <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 30px;'>
                <div style='max-width: 500px; margin: auto; background: #ffffff; border-radius: 8px; padding: 30px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
                    <h2 style='color: #2c3e50; text-align: center;'>Vista Core</h2>
                    <p style='font-size: 16px; color: #333;'>Ihr Bestätigungscode lautet:</p>
                    <div style='text-align: center; margin: 20px 0;'>
                        <span style='font-size: 32px; font-weight: bold; letter-spacing: 6px; color: #2980b9; background: #ecf0f1; padding: 12px 24px; border-radius: 6px;'>{code}</span>
                    </div>
                    <p style='font-size: 14px; color: #777;'>Dieser Code ist <strong>5 Minuten</strong> gültig.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #aaa; text-align: center;'>Falls Sie diese Anfrage nicht gestellt haben, ignorieren Sie bitte diese E-Mail.</p>
                </div>
            </body>
            </html>";

        await SendAsync(empfaengerEmail, betreff, inhalt);
    }

    public async Task SendStatusChangeAsync(string empfaengerEmail, string ticketTitel, string alterStatus, string neuerStatus)
    {
        var betreff = $"Vista — Ticketstatus geändert: {ticketTitel}";
        var inhalt = $@"
            <html>
            <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 30px;'>
                <div style='max-width: 500px; margin: auto; background: #ffffff; border-radius: 8px; padding: 30px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
                    <h2 style='color: #2c3e50; text-align: center;'>Vista Core</h2>
                    <p style='font-size: 16px; color: #333;'>Der Status des Tickets <strong>{ticketTitel}</strong> wurde geändert:</p>
                    <div style='text-align: center; margin: 20px 0;'>
                        <span style='color: #e74c3c; font-weight: bold;'>{alterStatus}</span>
                        <span style='margin: 0 10px;'>→</span>
                        <span style='color: #27ae60; font-weight: bold;'>{neuerStatus}</span>
                    </div>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #aaa; text-align: center;'>Diese E-Mail wurde automatisch generiert.</p>
                </div>
            </body>
            </html>";

        await SendAsync(empfaengerEmail, betreff, inhalt);
    }

    private async Task SendAsync(string empfaengerEmail, string betreff, string htmlInhalt)
    {
        var smtp = _configuration.GetSection("SmtpSettings");

        var smtpUser = smtp["User"] ?? throw new InvalidOperationException("SMTP User is not configured");
        var smtpHost = smtp["Host"] ?? throw new InvalidOperationException("SMTP Host is not configured");
        var smtpPass = smtp["Pass"] ?? throw new InvalidOperationException("SMTP Pass is not configured");
        var smtpPort = int.Parse(smtp["Port"] ?? "587");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Vista Core", smtpUser));
        message.To.Add(MailboxAddress.Parse(empfaengerEmail));
        message.Subject = betreff;
        message.Body = new TextPart("html") { Text = htmlInhalt };

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPass);
            await client.SendAsync(message);
            _logger.LogInformation("E-Mail an {Email} gesendet", empfaengerEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-Mail-Versand an {Email} fehlgeschlagen", empfaengerEmail);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
}
