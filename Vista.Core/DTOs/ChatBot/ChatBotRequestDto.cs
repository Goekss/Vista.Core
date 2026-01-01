namespace Vista.Core.DTOs.ChatBot;

public class ChatBotRequestDto
{
    public string Nachricht { get; set; } = string.Empty;
    public string? Kontext { get; set; }
}
