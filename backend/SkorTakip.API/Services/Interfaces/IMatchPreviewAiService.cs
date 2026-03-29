using SkorTakip.API.DTOs;

namespace SkorTakip.API.Services.Interfaces;

/// <summary>
/// Oynanacak maçlar için kısa AI önizlemesi — AI sohbet (IAiChatService) ile ayrı entegrasyon.
/// </summary>
public interface IMatchPreviewAiService
{
    Task<MatchPreviewResponseDto> GetPreviewAsync(MatchPreviewRequestDto request, CancellationToken ct = default);
}
