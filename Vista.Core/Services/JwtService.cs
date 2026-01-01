using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Vista.Core.Data;
using Vista.Core.DTOs.Auth;
using Vista.Core.Models;

namespace Vista.Core.Services;

public class JwtService
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;

    public JwtService(IConfiguration configuration, AppDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    public AuthResponseDto GenerateTokens(Benutzer benutzer, IList<string> rollen)
    {
        var ablauf = DateTime.UtcNow.AddMinutes(15);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, benutzer.Id),
            new(ClaimTypes.Email, benutzer.Email ?? string.Empty),
            new("Vorname", benutzer.Vorname),
            new("Nachname", benutzer.Nachname),
            new("MandantId", benutzer.MandantId),
            new("Rolle", benutzer.Rolle)
        };

        foreach (var rolle in rollen)
        {
            claims.Add(new Claim(ClaimTypes.Role, rolle));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: ablauf,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshToken = new RefreshToken
        {
            Token = GenerateRefreshTokenString(),
            BenutzerId = benutzer.Id,
            AblaufDatum = DateTime.UtcNow.AddDays(7),
            IstWiderrufen = false
        };

        _context.RefreshTokens.Add(refreshToken);
        _context.SaveChanges();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            AccessTokenAblauf = ablauf
        };
    }

    /// <summary>
    /// RefreshToken rotation: eski token iptal, yeni token üret (ADIM 3.4)
    /// </summary>
    public async Task<AuthResponseDto?> RotateRefreshTokenAsync(string token)
    {
        var existing = await _context.RefreshTokens
            .Include(r => r.Benutzer)
            .FirstOrDefaultAsync(r => r.Token == token);

        if (existing is null || existing.IstWiderrufen || existing.AblaufDatum <= DateTime.UtcNow)
            return null;

        // Eski token'ı iptal et
        existing.IstWiderrufen = true;

        // Yeni token üret
        var newRefreshToken = new RefreshToken
        {
            Token = GenerateRefreshTokenString(),
            BenutzerId = existing.BenutzerId,
            AblaufDatum = DateTime.UtcNow.AddDays(7),
            IstWiderrufen = false
        };

        existing.ErsetztDurch = newRefreshToken.Token;

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        // Yeni access + refresh token döndür
        var benutzer = existing.Benutzer;
        var ablauf = DateTime.UtcNow.AddMinutes(15);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, benutzer.Id),
            new(ClaimTypes.Email, benutzer.Email ?? string.Empty),
            new("Vorname", benutzer.Vorname),
            new("Nachname", benutzer.Nachname),
            new("MandantId", benutzer.MandantId),
            new("Rolle", benutzer.Rolle)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwtToken = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: ablauf,
            signingCredentials: creds);

        return new AuthResponseDto
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken),
            RefreshToken = newRefreshToken.Token,
            AccessTokenAblauf = ablauf
        };
    }

    public async Task RevokeAllTokensAsync(string benutzerId)
    {
        var tokens = await _context.RefreshTokens
            .Where(r => r.BenutzerId == benutzerId && !r.IstWiderrufen)
            .ToListAsync();

        foreach (var t in tokens)
            t.IstWiderrufen = true;

        await _context.SaveChangesAsync();
    }

    private static string GenerateRefreshTokenString()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
