namespace SkorTakip.API.DTOs;

public class TeamProfileDto
{
    public int     Id       { get; set; }
    public string  Name     { get; set; } = string.Empty;
    public string? Code     { get; set; }
    public string? Country  { get; set; }
    public int?    Founded  { get; set; }
    public bool    National { get; set; }
    public string? Logo     { get; set; }
    public VenueDto? Venue  { get; set; }
    public List<SquadPlayerDto> Squad { get; set; } = [];
}

public class VenueDto
{
    public int?    Id       { get; set; }
    public string? Name     { get; set; }
    public string? Address  { get; set; }
    public string? City     { get; set; }
    public int?    Capacity { get; set; }
    public string? Surface  { get; set; }
    public string? Image    { get; set; }
}

public class SquadPlayerDto
{
    public int     Id       { get; set; }
    public string  Name     { get; set; } = string.Empty;
    public int?    Age      { get; set; }
    public int?    Number   { get; set; }
    public string? Position { get; set; }
    public string? Photo    { get; set; }
}
