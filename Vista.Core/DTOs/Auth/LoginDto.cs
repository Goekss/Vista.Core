namespace Vista.Core.DTOs.Auth;

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Passwort { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public bool ZweiFaktorErforderlich { get; set; }
    public string Nachricht { get; set; } = string.Empty;
}
