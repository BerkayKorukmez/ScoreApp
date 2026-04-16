namespace SkorTakip.API.DTOs;

public class GoalKingDto
{
    public int    PlayerId { get; set; }
    public string Name     { get; set; } = string.Empty;
    public string? Photo   { get; set; }
    public string? Team    { get; set; }
    public string? TeamLogo { get; set; }
    public int    Goals    { get; set; }
}
