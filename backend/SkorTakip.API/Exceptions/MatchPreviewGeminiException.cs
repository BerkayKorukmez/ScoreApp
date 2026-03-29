namespace SkorTakip.API.Exceptions;

/// <summary>
/// Gemini generateContent upstream HTTP hatası — API yanıtında aynı status kodu kullanılır (örn. 429 kota).
/// </summary>
public sealed class MatchPreviewGeminiException : InvalidOperationException
{
    public int HttpStatus { get; }

    public MatchPreviewGeminiException(int httpStatus, string message) : base(message)
    {
        HttpStatus = httpStatus is >= 400 and <= 599 ? httpStatus : 503;
    }
}
