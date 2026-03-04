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
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.SportType).HasConversion<int>();
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
        }
    }
