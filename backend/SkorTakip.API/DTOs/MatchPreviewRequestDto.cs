namespace SkorTakip.API.DTOs;

public class MatchPreviewRequestDto
{
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    /// <summary>Lig adı (opsiyonel)</summary>
    public string? LeagueName { get; set; }
    /// <summary>football | basketball | volleyball</summary>
    public string Sport { get; set; } = "football";
}
