namespace SkorTakip.API.DTOs;

/// <summary>
/// CollectAPI sport/goalKings yanıtı.
/// </summary>
public class GoalKingDto
{
    public string Name  { get; set; } = string.Empty;
    public int    Goals { get; set; }
}
