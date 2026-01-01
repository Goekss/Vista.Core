using Microsoft.Extensions.Caching.Distributed;

namespace Vista.Core.Services;

public class ZweiFaktorService
{
    private readonly IDistributedCache _cache;
    private const int CodeLaenge = 6;
    private static readonly TimeSpan CodeGueltigkeitsDauer = TimeSpan.FromMinutes(5);

    public ZweiFaktorService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<string> CodeGenerierenAsync(string email)
    {
        var code = Random.Shared.Next(100000, 999999).ToString();
        var key = GetCacheKey(email);

        await _cache.SetStringAsync(key, code, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CodeGueltigkeitsDauer
        });

        return code;
    }

    public async Task<bool> CodeVerifizierenAsync(string email, string code)
    {
        var key = GetCacheKey(email);
        var gespeicherterCode = await _cache.GetStringAsync(key);

        if (gespeicherterCode is null || gespeicherterCode != code)
            return false;

        await _cache.RemoveAsync(key);
        return true;
    }

    private static string GetCacheKey(string email) => $"2fa:{email.ToLowerInvariant()}";
}
