namespace SkorTakip.API.DTOs;

public class MatchPreviewResponseDto
{
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public int HomeWinPercent { get; set; }
    public int DrawPercent { get; set; }
    public int AwayWinPercent { get; set; }
    /// <summary>Kısa karşılaştırma metni (Türkçe)</summary>
    public string Analysis { get; set; } = string.Empty;
}
