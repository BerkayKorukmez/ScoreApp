using Microsoft.AspNetCore.Mvc;
using SkorTakip.API.Services.Interfaces;

namespace SkorTakip.API.Controllers;

[ApiController]
[Route("api/team")]
public class TeamController : ControllerBase
{
    private readonly IExternalApiService _externalApiService;

    public TeamController(IExternalApiService externalApiService)
    {
        _externalApiService = externalApiService;
    }

    /// <summary>
    /// Kulüp profili: temel bilgiler + stadyum + güncel kadro.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetTeamProfile(int id)
    {
        if (id <= 0) return BadRequest("Geçersiz takım ID.");
        var profile = await _externalApiService.FetchTeamProfileAsync(id);
        if (profile is null) return NotFound("Takım bulunamadı.");
        return Ok(profile);
    }
}
