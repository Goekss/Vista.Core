using Microsoft.Extensions.Caching.Distributed;
using Microsoft.SemanticKernel;

namespace Vista.Core.Services.ChatBot;

/// <summary>
/// Begrenzt die Auto-Function-Calling-Schleife (Token-Budget / Groq Free Tier).
/// </summary>
public class MaxToolCallsFilter : IAutoFunctionInvocationFilter
{
    private const int MaxRunden = 2;

    public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
    {
        await next(context);
        if (context.RequestSequenceIndex >= MaxRunden - 1)
            context.Terminate = true;
    }
}

public class ChatInputFilter
{
    private static readonly string[] VerboteneWoerter = [
        "passwort", "password", "şifre", "iban", "kreditkarte", "credit card"
    ];

    public (bool IstErlaubt, string Grund) Validieren(string eingabe)
    {
        if (string.IsNullOrWhiteSpace(eingabe))
            return (false, "Leere Nachricht.");

        if (eingabe.Length > 1000)
            return (false, "Nachricht ist zu lang (max 1000 Zeichen).");

        var lower = eingabe.ToLowerInvariant();
        foreach (var wort in VerboteneWoerter)
        {
            if (lower.Contains(wort))
                return (false, "Anfragen zu sensiblen Daten sind nicht erlaubt.");
        }

        return (true, string.Empty);
    }
}

public class ChatRateLimiter
{
    private readonly IDistributedCache _cache;
    private const int MaxAnfragenProTag = 100;

    public ChatRateLimiter(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<(bool IstErlaubt, int Verbleibend)> PruefeLimit(Guid mandantId)
    {
        var schluessel = $"vika:limit:{mandantId}:{DateTime.UtcNow:yyyyMMdd}";
        var wert = await _cache.GetStringAsync(schluessel);
        var aktuelleAnzahl = string.IsNullOrEmpty(wert) ? 0 : int.Parse(wert);

        if (aktuelleAnzahl >= MaxAnfragenProTag)
            return (false, 0);

        aktuelleAnzahl++;
        await _cache.SetStringAsync(schluessel, aktuelleAnzahl.ToString(),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            });

        return (true, MaxAnfragenProTag - aktuelleAnzahl);
    }
}

public class ChatOutputFilter
{
    private static readonly string[] SensibleMuster = [
        @"\b[A-Z]{2}\d{2}\s?\d{4}\s?\d{4}\s?\d{4}\s?\d{4}\s?\d{0,2}\b", // IBAN
        @"\b\d{16}\b", // Kreditkarte
        @"passwort\s*[:=]\s*\S+", // Passwort
        @"şifre\s*[:=]\s*\S+", // Şifre
        @"token\s*[:=]\s*\S+", // Token
    ];

    private static readonly string[] LeakMuster = [
        @"yasaklarımıza?\s+göre",
        @"kurallar(ım(ız)?a?|ıma)\s+göre",
        @"sistem\s+prompt",
        @"talimat(lar)?ım(a|da)",
        @"fonksiyon\s+çağırma(malı(yd[ıi]m)?|m\s+yok)",
        @"\(Note:.*?\)",
    ];

    public string Filtern(string antwort)
    {
        foreach (var muster in SensibleMuster)
        {
            antwort = System.Text.RegularExpressions.Regex.Replace(
                antwort, muster, "[GEFILTERT]",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        foreach (var muster in LeakMuster)
        {
            antwort = System.Text.RegularExpressions.Regex.Replace(
                antwort, muster, "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
        }

        // JSON-Anteile entfernen, falls Modell trotz Prompt durchlässt
        antwort = System.Text.RegularExpressions.Regex.Replace(
            antwort, @"\{[^{}]*""(name|parameters|function)""[^{}]*\}", "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return antwort.Trim();
    }
}
