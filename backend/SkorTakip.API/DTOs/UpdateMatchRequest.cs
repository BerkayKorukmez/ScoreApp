using SkorTakip.API.Models;

namespace SkorTakip.API.DTOs;

public class UpdateMatchRequest
{
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public int Minute { get; set; }
    public MatchStatus Status { get; set; }
}
