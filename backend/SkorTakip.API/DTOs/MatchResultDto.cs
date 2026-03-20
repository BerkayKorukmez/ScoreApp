namespace SkorTakip.API.DTOs;

/// <summary>
/// CollectAPI'den gelen maç sonucu (son hafta).
/// </summary>
public class MatchResultDto
{
    public string HomeTeam  { get; set; } = string.Empty;
    public string AwayTeam  { get; set; } = string.Empty;
    public int?   HomeScore { get; set; }
    public int?   AwayScore { get; set; }
    public string Date      { get; set; } = string.Empty;
    /// <summary>Maç oynanmış mı? Skor undefined ise false.</summary>
    public bool   IsPlayed  { get; set; }
}
