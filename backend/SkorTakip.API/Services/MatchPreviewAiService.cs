using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using SkorTakip.API.DTOs;
using SkorTakip.API.Exceptions;
using SkorTakip.API.Services.Interfaces;

namespace SkorTakip.API.Services;

/// <summary>
/// Maç önizlemesi için Gemini çağrıları — <see cref="AiChatService"/> ile paylaşılmaz, DB kaydı yok.
/// </summary>
public class MatchPreviewAiService : IMatchPreviewAiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MatchPreviewAiService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public MatchPreviewAiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<MatchPreviewAiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<MatchPreviewResponseDto> GetPreviewAsync(MatchPreviewRequestDto request, CancellationToken ct = default)
    {
        var apiKey = _configuration["MatchPreview:GeminiApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            apiKey = _configuration["Gemini:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("MatchPreview:GeminiApiKey ve Gemini:ApiKey boş.");
            throw new InvalidOperationException("Maç önizlemesi için API anahtarı yapılandırılmamış.");
        }

        var home = (request.HomeTeam ?? "").Trim();
        var away = (request.AwayTeam ?? "").Trim();
        if (home.Length < 2 || away.Length < 2)
            throw new ArgumentException("Takım adları geçersiz.");

        var league = string.IsNullOrWhiteSpace(request.LeagueName) ? "" : request.LeagueName.Trim();
        var sport = string.IsNullOrWhiteSpace(request.Sport) ? "football" : request.Sport.Trim().ToLowerInvariant();

        var schemaHint = JsonSerializer.Serialize(new
        {
            homeTeam = home,
            awayTeam = away,
            homeWinPercent = 33,
            drawPercent = 34,
            awayWinPercent = 33,
            analysis = "Türkçe kısa karşılaştırma (2-4 cümle); tahmin olduğunu belirt."
        });

        var prompt = $"""
            Sen bir spor analistisin. Aşağıdaki yaklaşan maç için iki takımı genel form, kadro derinliği ve üslup açısından karşılaştır.
            Canlı istatistik veya güncel sakat listesine erişimin yok; genel bilgilerle makul bir analiz yap.
            Tahmin olduğunu vurgula.

            Spor: {sport}
            Lig: {(string.IsNullOrEmpty(league) ? "belirtilmedi" : league)}
            Ev sahibi: {home}
            Deplasman: {away}

            Yanıtı YALNIZCA geçerli JSON olarak ver (markdown veya ek metin yok). Şema örneği (sayıları ve analysis metnini kendi analizine göre güncelle):
            {schemaHint}
            homeWinPercent, drawPercent, awayWinPercent tam sayı ve toplamları 100 olmalı.
            """;

        var model = _configuration["MatchPreview:Model"] ?? _configuration["Gemini:Model"] ?? "gemini-3.1-flash-lite-preview";
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var payload = new JsonObject
        {
            ["contents"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = new JsonArray { new JsonObject { ["text"] = prompt } }
                }
            },
            ["generationConfig"] = new JsonObject
            {
                ["temperature"] = 0.55,
                ["maxOutputTokens"] = 1024,
                ["topP"] = 0.9
            }
        };

        var json = JsonSerializer.Serialize(payload);

        const int maxAttempts = 3;
        string body = "";
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(url, requestContent, ct);
            var lastStatus = response.StatusCode;
            body = await response.Content.ReadAsStringAsync(ct);
            if (response.IsSuccessStatusCode)
                break;

            if (attempt < maxAttempts && IsTransientGeminiFailure(lastStatus))
            {
                _logger.LogWarning(
                    "MatchPreview Gemini geçici hata {Status}, deneme {Attempt}/{Max}, gövde: {Body}",
                    lastStatus, attempt, maxAttempts, body.Length > 200 ? body[..200] : body);
                await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt), ct);
                continue;
            }

            _logger.LogError("MatchPreview Gemini hata {Status}: {Body}", lastStatus,
                body.Length > 300 ? body[..300] : body);
            throw new MatchPreviewGeminiException((int)lastStatus, UserMessageForGeminiHttpFailure(lastStatus));
        }

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            if (doc.RootElement.TryGetProperty("promptFeedback", out var fb))
            {
                var raw = fb.GetRawText();
                _logger.LogWarning("MatchPreview promptFeedback: {Fb}", raw.Length > 400 ? raw[..400] : raw);
            }
            throw new InvalidOperationException(
                "Model bu maç için yanıt üretemedi (içerik filtresi veya boş çıktı). Başka bir maçta tekrar deneyin.");
        }

        var first = candidates[0];
        if (!first.TryGetProperty("content", out var contentEl) ||
            !contentEl.TryGetProperty("parts", out var parts) ||
            parts.GetArrayLength() == 0)
        {
            if (first.TryGetProperty("finishReason", out var fr))
                _logger.LogWarning("MatchPreview finishReason: {Reason}", fr.GetString());
            throw new InvalidOperationException(
                "Model çıktısı okunamadı (güvenlik veya format). Tekrar deneyin.");
        }

        var firstPart = parts[0];
        if (!firstPart.TryGetProperty("text", out var textEl))
        {
            _logger.LogWarning("MatchPreview parts[0] metin içermiyor: {Raw}", firstPart.GetRawText());
            throw new InvalidOperationException(
                "Model metin üretmedi. Bir süre sonra tekrar deneyin.");
        }

        var rawText = textEl.GetString() ?? "";
        rawText = StripJsonFence(rawText).Trim();

        MatchPreviewResponseDto? parsed = null;
        try
        {
            parsed = JsonSerializer.Deserialize<MatchPreviewResponseDto>(rawText, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MatchPreview JSON parse edilemedi, ham metin döndürülüyor");
        }

        if (parsed == null || string.IsNullOrWhiteSpace(parsed.Analysis))
        {
            return new MatchPreviewResponseDto
            {
                HomeTeam = home,
                AwayTeam = away,
                HomeWinPercent = 34,
                DrawPercent = 33,
                AwayWinPercent = 33,
                Analysis = rawText.Length > 0 ? rawText : "Analiz üretilemedi."
            };
        }

        parsed.HomeTeam = string.IsNullOrWhiteSpace(parsed.HomeTeam) ? home : parsed.HomeTeam;
        parsed.AwayTeam = string.IsNullOrWhiteSpace(parsed.AwayTeam) ? away : parsed.AwayTeam;
        NormalizePercents(parsed);
        return parsed;
    }

    private static void NormalizePercents(MatchPreviewResponseDto r)
    {
        var h = Math.Clamp(r.HomeWinPercent, 0, 100);
        var d = Math.Clamp(r.DrawPercent, 0, 100);
        var a = Math.Clamp(r.AwayWinPercent, 0, 100);
        var sum = h + d + a;
        if (sum == 0)
        {
            r.HomeWinPercent = 34;
            r.DrawPercent = 33;
            r.AwayWinPercent = 33;
            return;
        }

        if (sum != 100)
        {
            r.HomeWinPercent = (int)Math.Round(h * 100.0 / sum);
            r.DrawPercent = (int)Math.Round(d * 100.0 / sum);
            r.AwayWinPercent = 100 - r.HomeWinPercent - r.DrawPercent;
            if (r.AwayWinPercent < 0)
            {
                r.AwayWinPercent = 0;
                r.DrawPercent = 100 - r.HomeWinPercent;
            }
        }
        else
        {
            r.HomeWinPercent = h;
            r.DrawPercent = d;
            r.AwayWinPercent = a;
        }
    }

    private static string StripJsonFence(string text)
    {
        var m = Regex.Match(text.Trim(), @"^```(?:json)?\s*([\s\S]*?)```$", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : text.Trim();
    }

    /// <summary>429 kota — tekrar denemek kotayı daha da zorlar; sadece sunucu taraflı geçici hatalar.</summary>
    private static bool IsTransientGeminiFailure(HttpStatusCode status) =>
        status == HttpStatusCode.BadGateway ||
        status == HttpStatusCode.ServiceUnavailable ||
        status == HttpStatusCode.GatewayTimeout;

    private static string UserMessageForGeminiHttpFailure(HttpStatusCode status) =>
        status switch
        {
            HttpStatusCode.TooManyRequests =>
                "Çok sık istek atıldı veya kota doldu. Bir süre sonra tekrar deneyin.",
            HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout =>
                "Yapay zeka servisi şu an yoğun veya geçici olarak kullanılamıyor. Lütfen tekrar deneyin.",
            _ => "Yapay zeka yanıtı alınamadı."
        };
}
