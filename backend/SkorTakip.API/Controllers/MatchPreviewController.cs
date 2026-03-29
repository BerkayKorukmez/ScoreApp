using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkorTakip.API.DTOs;
using SkorTakip.API.Exceptions;
using SkorTakip.API.Services.Interfaces;

namespace SkorTakip.API.Controllers;

/// <summary>
/// Oynanacak maçlar için AI önizleme — <c>/api/ai</c> sohbet uçlarından bağımsız.
/// </summary>
[ApiController]
[Route("api/match-preview")]
[Authorize]
public class MatchPreviewController : ControllerBase
{
    private readonly IMatchPreviewAiService _preview;

    public MatchPreviewController(IMatchPreviewAiService preview)
    {
        _preview = preview;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] MatchPreviewRequestDto request, CancellationToken ct)
    {
        if (request == null)
            return BadRequest(new { message = "İstek gövdesi gerekli." });

        try
        {
            var result = await _preview.GetPreviewAsync(request, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (MatchPreviewGeminiException ex)
        {
            return StatusCode(ex.HttpStatus, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { message = ex.Message });
        }
    }
}
