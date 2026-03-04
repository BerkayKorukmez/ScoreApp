using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkorTakip.API.Models;

[Table("FavoriteMatches")]
public class FavoriteMatch
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string MatchId { get; set; } = string.Empty;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ApplicationUser User { get; set; } = null!;
}

