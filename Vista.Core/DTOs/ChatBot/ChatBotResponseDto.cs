namespace Vista.Core.DTOs.ChatBot;

public class ChatBotResponseDto
{
    public string Antwort { get; set; } = string.Empty;
    public string? Quelle { get; set; }
    public double RelevanzScore { get; set; }
    public DateTime ZeitStempel { get; set; } = DateTime.UtcNow;
}
