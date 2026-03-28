namespace SkorTakip.API.DTOs;

/// <summary>
/// API-Sports fixtures/lineups yanıtının ev/deplasman olarak eşlenmiş hali.
/// </summary>
public class FootballMatchLineupsDto
{
    public FootballTeamLineupDto? Home { get; set; }
    public FootballTeamLineupDto? Away { get; set; }
}

public class FootballTeamLineupDto
{
    public string TeamName { get; set; } = string.Empty;
    public string? Formation { get; set; }
    public List<LineupPlayerDto> StartingXI { get; set; } = new();
}

public class LineupPlayerDto
{
    public int? Number { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>API: G, D, M, F</summary>
    public string? Position { get; set; }
    public string? Photo { get; set; }
}
