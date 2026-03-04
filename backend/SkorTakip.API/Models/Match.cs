using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SkorTakip.API.Models;

[Table("Matches")]
public class Match
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(200)]
    public string HomeTeam { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string AwayTeam { get; set; } = string.Empty;
    
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public int Minute { get; set; }
    
    [MaxLength(200)]
    public string League { get; set; } = string.Empty;
    
    public DateTime StartTime { get; set; }
    public MatchStatus Status { get; set; }
    public SportType SportType { get; set; } = SportType.Football;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Harici API fixture ID'si (DB'ye kaydedilmez)
    [NotMapped]
    [JsonIgnore]
    public int? ExternalFixtureId { get; set; }

    // Maç istatistikleri (DB'ye kaydedilmez, sadece detay sayfasında döndürülür)
    [NotMapped]
    public Dictionary<string, object>? Statistics { get; set; }
}

public enum MatchStatus
{
    NotStarted = 0,
    Live = 1,
    HalfTime = 2,
    Finished = 3
}

public enum SportType
{
    Football = 0,
    Basketball = 1,
    AmericanFootball = 2,
    Volleyball = 3,
    Tennis = 4
}
