namespace SkorTakip.API.DTOs;

public class TeamSearchResultDto
{
    public int    Id      { get; set; }
    public string Name    { get; set; } = string.Empty;
    public string? Logo   { get; set; }
    public string? Country { get; set; }
    public string? Flag   { get; set; }
    /// <summary>"team" veya "league"</summary>
    public string Kind    { get; set; } = "team";
}
