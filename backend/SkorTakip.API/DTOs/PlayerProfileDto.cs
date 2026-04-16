namespace SkorTakip.API.DTOs;

public class PlayerProfileDto
{
    public int     Id          { get; set; }
    public string  Name        { get; set; } = string.Empty;
    public string? FirstName   { get; set; }
    public string? LastName    { get; set; }
    public int?    Age         { get; set; }
    public string? BirthDate   { get; set; }
    public string? BirthPlace  { get; set; }
    public string? BirthCountry { get; set; }
    public string? Nationality { get; set; }
    public string? Height      { get; set; }
    public string? Weight      { get; set; }
    public bool    Injured     { get; set; }
    public string? Photo       { get; set; }
    public List<PlayerSeasonStatsDto> Stats { get; set; } = [];
}

public class PlayerSeasonStatsDto
{
    public int?    TeamId        { get; set; }
    public string? TeamName      { get; set; }
    public string? TeamLogo      { get; set; }
    public int?    LeagueId      { get; set; }
    public string? LeagueName    { get; set; }
    public string? LeagueLogo    { get; set; }
    public string? LeagueFlag    { get; set; }
    public int?    Season        { get; set; }
    public string? Position      { get; set; }
    public int?    Appearances   { get; set; }
    public int?    Lineups       { get; set; }
    public int?    Minutes       { get; set; }
    public int?    Goals         { get; set; }
    public int?    Assists       { get; set; }
    public int?    YellowCards   { get; set; }
    public int?    RedCards      { get; set; }
    public int?    ShotsTotal    { get; set; }
    public int?    ShotsOn       { get; set; }
    public int?    DribblesAttempts { get; set; }
    public int?    DribblesSuccess  { get; set; }
    public string? Rating         { get; set; }
}
