using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using SkorTakip.API.Services.Interfaces;

namespace SkorTakip.API.Services;

public class AiChatService : IAiChatService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiChatService> _logger;

    private const string SystemPrompt = """
        Sen spor odaklı bir asistanısın. Kullanıcılar sana futbol, basketbol, voleybol ve diğer sporlar hakkında sorular soracak.
        Örnek sorular: "Galatasaray maçı ne olur?", "Bu hafta hangi maçlar var?", "Messi kaç gol attı?" gibi.
        Yanıtlarını Türkçe ve kısa tut. Tahmin gerektiren sorularda makul tahminler yap ama "tahmin" olduğunu belirt.
        Güncel verilere erişimin yok; genel bilgiler ve analiz sun. Spor dışı konularda kibarca spor konularına yönlendir.
        """;

    public AiChatService(HttpClient httpClient, IConfiguration configuration, ILogger<AiChatService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> SendMessageAsync(string userMessage, CancellationToken ct = default)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Gemini:ApiKey yapılandırılmamış.");
            return "Yapay zeka servisi şu an kullanılamıyor. Lütfen daha sonra tekrar deneyin.";
        }

        if (string.IsNullOrWhiteSpace(userMessage))
            return "Lütfen bir soru yazın.";

        userMessage = userMessage.Trim();
        if (userMessage.Length > 2000)
            userMessage = userMessage[..2000];

        try
        {
            // Gemini REST API: https://ai.google.dev/gemini-api/docs
            var model = _configuration["Gemini:Model"] ?? "gemini-3.1-flash-lite-preview";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var payload = new JsonObject
            {
                ["contents"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["role"] = "user",
                        ["parts"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["text"] = $"{SystemPrompt}\n\nKullanıcı sorusu: {userMessage}"
                            }
                        }
                    }
                },
                ["generationConfig"] = new JsonObject
                {
                    ["temperature"] = 0.7,
                    ["maxOutputTokens"] = 1024,
                    ["topP"] = 0.9
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Gemini API hata {Status}: {Body}", response.StatusCode, errBody.Length > 200 ? errBody[..200] : errBody);
                return "Yanıt alınırken bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
            }

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseBody);

            var root = doc.RootElement;
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var first = candidates[0];
                if (first.TryGetProperty("content", out var contentEl) &&
                    contentEl.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    var text = parts[0].GetProperty("text").GetString() ?? "";
                    return text.Trim();
                }
            }

            _logger.LogWarning("Gemini yanıt formatı beklenmeyen: {Preview}", responseBody.Length > 300 ? responseBody[..300] : responseBody);
            return "Yanıt işlenemedi. Lütfen sorunuzu tekrar ifade edin.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API çağrı hatası");
            return "Bağlantı hatası oluştu. Lütfen daha sonra tekrar deneyin.";
        }
    }
}
