using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SkorTakip.API.DTOs;

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

    // Lig ülkesi (DB'ye kaydedilir)
    [MaxLength(100)]
    public string? LeagueCountry { get; set; }

    // Harici API lig ID'si (DB'ye kaydedilir, puan tablosu için kullanılır)
    public int? ExternalLeagueId { get; set; }

    // Ülke bayrağı URL'si (DB'ye kaydedilir)
    [MaxLength(500)]
    public string? LeagueFlag { get; set; }

    public DateTime StartTime { get; set; }
    public MatchStatus Status { get; set; }
    public SportType SportType { get; set; } = SportType.Football;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Harici API fixture ID'si (DB'ye kaydedilmez - zaten Id içinde gömülü)
    [NotMapped]
    [JsonIgnore]
    public int? ExternalFixtureId { get; set; }

    // Takım logoları (DB'ye kaydedilir)
    [MaxLength(500)]
    public string? HomeTeamLogo { get; set; }

    [MaxLength(500)]
    public string? AwayTeamLogo { get; set; }

    /// <summary>
    /// Admin tarafından gizlenen maçlar kullanıcılara gösterilmez.
    /// </summary>
    public bool IsHidden { get; set; } = false;

    // Maç istatistikleri (DB'ye kaydedilmez, sadece detay sayfasında döndürülür)
    [NotMapped]
    public Dictionary<string, object>? Statistics { get; set; }

    // Maç olayları - goller, kartlar, değişiklikler (DB'ye kaydedilmez)
    [NotMapped]
    public List<Dictionary<string, object>>? Events { get; set; }

    // Futbol ilk 11 kadroları (DB'ye kaydedilmez)
    [NotMapped]
    public FootballMatchLineupsDto? Lineups { get; set; }

    /// <summary>
    /// Stadyum adı (API-Sports venue.name; DB'ye kaydedilmez).
    /// </summary>
    [NotMapped]
    [MaxLength(200)]
    [JsonPropertyName("stadiumName")]
    public string? StadiumName { get; set; }
}
