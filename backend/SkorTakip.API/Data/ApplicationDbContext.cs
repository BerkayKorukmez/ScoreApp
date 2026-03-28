using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkorTakip.API.Models;

namespace SkorTakip.API.Data;

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Match> Matches { get; set; }
        public DbSet<FavoriteMatch> FavoriteMatches { get; set; }
        public DbSet<AiChatMessage> AiChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Match>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.HomeTeam).IsRequired().HasMaxLength(200);
                entity.Property(e => e.AwayTeam).IsRequired().HasMaxLength(200);
                entity.Property(e => e.League).HasMaxLength(200);
                entity.Property(e => e.LeagueCountry).HasMaxLength(100);
                entity.Property(e => e.LeagueFlag).HasMaxLength(500);
                entity.Property(e => e.HomeTeamLogo).HasMaxLength(500);
                entity.Property(e => e.AwayTeamLogo).HasMaxLength(500);
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.SportType).HasConversion<int>();
                entity.Property(e => e.IsHidden).HasDefaultValue(false);
            });

            builder.Entity<FavoriteMatch>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MatchId).IsRequired().HasMaxLength(100);
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.UserId, e.MatchId }).IsUnique();
            });

            builder.Entity<AiChatMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Role).HasMaxLength(20);
                entity.Property(e => e.Text).HasMaxLength(8000);
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
            });
        }
    }
