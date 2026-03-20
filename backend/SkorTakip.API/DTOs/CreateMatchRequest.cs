using SkorTakip.API.Models;

namespace SkorTakip.API.DTOs;

public class CreateMatchRequest
{
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public string League { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public SportType SportType { get; set; } = SportType.Football;
}
