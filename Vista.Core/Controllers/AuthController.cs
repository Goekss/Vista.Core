using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vista.Core.Data;
using Vista.Core.DTOs.Auth;
using Vista.Core.Models;
using Vista.Core.Services;

namespace Vista.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<Benutzer> _userManager;
    private readonly SignInManager<Benutzer> _signInManager;
    private readonly JwtService _jwtService;
    private readonly ZweiFaktorService _zweiFaktorService;
    private readonly EmailService _emailService;
    private readonly ILogger<AuthController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly AppDbContext _db;

    public AuthController(
        UserManager<Benutzer> userManager,
        SignInManager<Benutzer> signInManager,
        JwtService jwtService,
        ZweiFaktorService zweiFaktorService,
        EmailService emailService,
        ILogger<AuthController> logger,
        IWebHostEnvironment env,
        AppDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _zweiFaktorService = zweiFaktorService;
        _emailService = emailService;
        _logger = logger;
        _env = env;
        _db = db;
    }

    /// <summary>
    /// Aşama 1: Email + Şifre → 2FA kodu gönderilir
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var benutzer = await _userManager.FindByEmailAsync(dto.Email);
        if (benutzer is null)
            return Unauthorized(new LoginResponseDto { Nachricht = "Ungültige Anmeldedaten." });

        if (await _userManager.IsLockedOutAsync(benutzer))
            return Unauthorized(new LoginResponseDto { Nachricht = "Konto ist gesperrt. Bitte versuchen Sie es später erneut." });

        var result = await _signInManager.CheckPasswordSignInAsync(benutzer, dto.Passwort, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Unauthorized(new LoginResponseDto { Nachricht = "Ungültige Anmeldedaten." });

        var code = await _zweiFaktorService.CodeGenerierenAsync(dto.Email);

        try
        {
            await _emailService.SendVerificationCodeAsync(dto.Email, code);
            _logger.LogInformation("2FA-Code an {Email} gesendet", dto.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-Mail-Versand fehlgeschlagen für {Email}", dto.Email);
            return StatusCode(500, new LoginResponseDto { Nachricht = "Verifizierungscode konnte nicht gesendet werden." });
        }

        return Ok(new LoginResponseDto
        {
            ZweiFaktorErforderlich = true,
            Nachricht = "Bestätigungscode wurde an Ihre E-Mail gesendet."
        });
    }

    /// <summary>
    /// Aşama 2: Email + Code → JWT + RefreshToken (httpOnly Cookie)
    /// </summary>
    [HttpPost("verify")]
    public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeRequestDto dto)
    {
        var istGueltig = await _zweiFaktorService.CodeVerifizierenAsync(dto.Email, dto.Code);
        if (!istGueltig)
            return Unauthorized(new { Nachricht = "Ungültiger oder abgelaufener Code." });

        var benutzer = await _userManager.FindByEmailAsync(dto.Email);
        if (benutzer is null)
            return Unauthorized(new { Nachricht = "Benutzer nicht gefunden." });

        var rollen = await _userManager.GetRolesAsync(benutzer);
        var tokens = _jwtService.GenerateTokens(benutzer, rollen);

        SetTokenCookies(tokens);

        _logger.LogInformation("Benutzer {Email} erfolgreich angemeldet", dto.Email);

        // MandantId artık kullanıcıdan geliyor
        return Ok(new 
        { 
            Nachricht = "Erfolgreich angemeldet.", 
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            AccessTokenAblauf = tokens.AccessTokenAblauf,
            Email = benutzer.Email,
            MandantId = benutzer.MandantId,
            Rolle = benutzer.Rolle,
            Vorname = benutzer.Vorname,
            Nachname = benutzer.Nachname
        });
    }

    /// <summary>
    /// RefreshToken rotation → yeni JWT + RefreshToken (httpOnly Cookie)
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { Nachricht = "Kein Refresh-Token vorhanden." });

        var tokens = await _jwtService.RotateRefreshTokenAsync(refreshToken);
        if (tokens is null)
            return Unauthorized(new { Nachricht = "Ungültiger oder abgelaufener Refresh-Token." });

        SetTokenCookies(tokens);

        // Token'ı response'da da döndür
        return Ok(new 
        { 
            Nachricht = "Token erfolgreich erneuert.", 
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            AccessTokenAblauf = tokens.AccessTokenAblauf
        });
    }

    /// <summary>
    /// Aktif oturumdaki kullanıcı bilgisi
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var benutzerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(benutzerId))
            return Unauthorized(new { Nachricht = "Benutzer nicht authentifiziert." });

        var benutzer = await _userManager.FindByIdAsync(benutzerId);
        if (benutzer is null)
            return Unauthorized(new { Nachricht = "Benutzer nicht gefunden." });

        var rollen = await _userManager.GetRolesAsync(benutzer);

        return Ok(new
        {
            benutzer.Id,
            benutzer.Email,
            benutzer.Vorname,
            benutzer.Nachname,
            benutzer.RufNummer,
            benutzer.Abteilung,
            benutzer.Rolle,
            benutzer.MandantId,
            Rollen = rollen
        });
    }

    /// <summary>
    /// Logout → tüm RefreshToken'lar iptal + Cookie temizle
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var benutzerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (benutzerId is null)
            return Unauthorized();

        await _jwtService.RevokeAllTokensAsync(benutzerId);

        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");

        _logger.LogInformation("Benutzer {BenutzerId} abgemeldet", benutzerId);

        return Ok(new { Nachricht = "Erfolgreich abgemeldet." });
    }

    /// <summary>
    /// Kilitli kullanıcıları listele (Admin/SuperAdmin)
    /// </summary>
    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet("locked-users")]
    public async Task<IActionResult> GetLockedUsers()
    {
        var alleBenutzer = _userManager.Users.ToList();
        var gesperrteBenutzer = new List<object>();

        foreach (var benutzer in alleBenutzer)
        {
            if (await _userManager.IsLockedOutAsync(benutzer))
            {
                gesperrteBenutzer.Add(new
                {
                    benutzer.Id,
                    benutzer.Email,
                    benutzer.Vorname,
                    benutzer.Nachname,
                    GesperrtBis = await _userManager.GetLockoutEndDateAsync(benutzer)
                });
            }
        }

        return Ok(gesperrteBenutzer);
    }

    /// <summary>
    /// Kilitli kullanıcı hesabını aç (Admin/SuperAdmin)
    /// </summary>
    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost("unlock/{email}")]
    public async Task<IActionResult> UnlockUser(string email)
    {
        var benutzer = await _userManager.FindByEmailAsync(email);
        if (benutzer is null)
            return NotFound(new { Nachricht = "Benutzer nicht gefunden." });

        await _userManager.SetLockoutEndDateAsync(benutzer, null);
        await _userManager.ResetAccessFailedCountAsync(benutzer);

        _logger.LogInformation("Benutzer {Email} wurde entsperrt", email);

        return Ok(new { Nachricht = $"Benutzer {email} wurde erfolgreich entsperrt." });
    }

    /// <summary>
    /// Token'ları httpOnly cookie olarak set eder (ADIM 3.10)
    /// </summary>
    private void SetTokenCookies(AuthResponseDto tokens)
    {
        var isDev = _env.IsDevelopment();

        Response.Cookies.Append("accessToken", tokens.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDev,
            SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.Strict,
            Path = "/",
            Expires = tokens.AccessTokenAblauf
        });

        Response.Cookies.Append("refreshToken", tokens.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDev,
            SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.Strict,
            Path = "/api/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }
}
