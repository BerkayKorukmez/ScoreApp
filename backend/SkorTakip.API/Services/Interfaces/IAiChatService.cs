namespace SkorTakip.API.Services.Interfaces;

public interface IAiChatService
{
    Task<string> SendMessageAsync(string userMessage, CancellationToken ct = default);
}
