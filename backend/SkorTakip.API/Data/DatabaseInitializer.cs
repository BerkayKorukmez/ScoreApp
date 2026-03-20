using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SkorTakip.API.Models;

namespace SkorTakip.API.Data;

/// <summary>
/// Uygulama başlangıcında veritabanını hazırlar:
/// tabloları kontrol eder / oluşturur ve gerekli şema migrasyonlarını uygular.
/// </summary>
public static class DatabaseInitializer
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

            logger.LogInformation("Veritabani hazirlaniyor...");

            EnsureTablesExist(context, logger);
            EnsureSportTypeColumn(context, logger);
            EnsureFavoriteMatchesTable(context, logger);
            EnsureMediaAndLeagueColumns(context, logger);
            EnsureIsHiddenColumn(context, logger);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
            logger.LogError(ex, "Database initialization hatasi: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Admin rolünü ve varsayılan admin kullanıcısını oluşturur (ilk çalıştırmada).
    /// appsettings.json içindeki "Admin" konfigürasyonunu kullanır.
    /// </summary>
    public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services       = scope.ServiceProvider;
        var roleManager    = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager    = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration  = services.GetRequiredService<IConfiguration>();
        var logger         = services.GetRequiredService<ILogger<ApplicationDbContext>>();

        const string adminRole = "Admin";

        // 1) Admin rolü yoksa oluştur
        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(adminRole));
            logger.LogInformation("'Admin' rolü oluşturuldu.");
        }

        // 2) Varsayılan admin kullanıcısı yoksa oluştur
        var adminEmail    = configuration["Admin:Email"]    ?? "admin@skortakip.com";
        var adminUserName = configuration["Admin:UserName"] ?? "admin";
        var adminPassword = configuration["Admin:Password"] ?? "Admin@1234";

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var admin = new ApplicationUser
            {
                UserName  = adminUserName,
                Email     = adminEmail,
                FirstName = "Admin",
                LastName  = "User",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, adminRole);
                logger.LogInformation("Varsayılan admin kullanıcısı oluşturuldu: {Email}", adminEmail);
            }
            else
            {
                logger.LogError("Admin kullanıcısı oluşturulamadı: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else if (!await userManager.IsInRoleAsync(existingAdmin, adminRole))
        {
            await userManager.AddToRoleAsync(existingAdmin, adminRole);
            logger.LogInformation("Mevcut kullanıcı admin yapıldı: {Email}", adminEmail);
        }
    }

    private static void EnsureTablesExist(ApplicationDbContext context, ILogger logger)
    {
        if (context.Database.CanConnect())
        {
            try
            {
                _ = context.Matches.Count();
                logger.LogInformation("Veritabani hazir, tablolar mevcut.");
            }
            catch
            {
                logger.LogInformation("Tablolar bulunamadi, olusturuluyor...");
                context.Database.EnsureCreated();
                logger.LogInformation("Tablolar olusturuldu!");
            }
        }
        else
        {
            logger.LogInformation("Veritabani olusturuluyor...");
            context.Database.EnsureCreated();
            logger.LogInformation("Veritabani olusturuldu!");
        }
    }

    private static void EnsureSportTypeColumn(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Matches tablosu SportType kolonu kontrol ediliyor...");

        const string sql = @"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'Matches'
          AND column_name = 'SportType'
    ) THEN
        ALTER TABLE ""Matches"" ADD COLUMN ""SportType"" integer NOT NULL DEFAULT 0;
    END IF;
END$$;";

        context.Database.ExecuteSqlRaw(sql);
        logger.LogInformation("Matches.SportType kolonu kontrol edildi / eklendi.");
    }

    /// <summary>
    /// Matches tablosuna logo ve lig detay kolonlarını ekler (varsa atlar).
    /// HomeTeamLogo, AwayTeamLogo, LeagueCountry, LeagueFlag, ExternalLeagueId
    /// </summary>
    private static void EnsureMediaAndLeagueColumns(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Matches tablosu medya/lig kolonlari kontrol ediliyor...");

        const string sql = @"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'Matches' AND column_name = 'HomeTeamLogo') THEN
        ALTER TABLE ""Matches"" ADD COLUMN ""HomeTeamLogo"" VARCHAR(500);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'Matches' AND column_name = 'AwayTeamLogo') THEN
        ALTER TABLE ""Matches"" ADD COLUMN ""AwayTeamLogo"" VARCHAR(500);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'Matches' AND column_name = 'LeagueCountry') THEN
        ALTER TABLE ""Matches"" ADD COLUMN ""LeagueCountry"" VARCHAR(100);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'Matches' AND column_name = 'LeagueFlag') THEN
        ALTER TABLE ""Matches"" ADD COLUMN ""LeagueFlag"" VARCHAR(500);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'Matches' AND column_name = 'ExternalLeagueId') THEN
        ALTER TABLE ""Matches"" ADD COLUMN ""ExternalLeagueId"" integer;
    END IF;
END$$;";

        context.Database.ExecuteSqlRaw(sql);
        logger.LogInformation("Matches medya/lig kolonlari kontrol edildi / eklendi.");
    }

    private static void EnsureIsHiddenColumn(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Matches.IsHidden kolonu kontrol ediliyor...");

        const string sql = @"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'Matches' AND column_name = 'IsHidden'
    ) THEN
        ALTER TABLE ""Matches"" ADD COLUMN ""IsHidden"" boolean NOT NULL DEFAULT false;
        RAISE NOTICE 'Matches.IsHidden kolonu eklendi.';
    END IF;
END$$;";

        context.Database.ExecuteSqlRaw(sql);
        logger.LogInformation("Matches.IsHidden kolonu hazir.");
    }

    private static void EnsureFavoriteMatchesTable(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("FavoriteMatches tablosu kontrol ediliyor...");

        const string sql = @"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'FavoriteMatches'
    ) THEN
        CREATE TABLE ""FavoriteMatches"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""UserId"" TEXT NOT NULL,
            ""MatchId"" VARCHAR(100) NOT NULL,
            ""AddedAt"" TIMESTAMP NOT NULL,
            CONSTRAINT ""FK_FavoriteMatches_AspNetUsers_UserId"" 
                FOREIGN KEY (""UserId"") 
                REFERENCES ""AspNetUsers"" (""Id"") 
                ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX ""IX_FavoriteMatches_UserId_MatchId"" 
            ON ""FavoriteMatches"" (""UserId"", ""MatchId"");
        RAISE NOTICE 'FavoriteMatches tablosu olusturuldu.';
    ELSE
        RAISE NOTICE 'FavoriteMatches tablosu zaten mevcut.';
    END IF;
END$$;";

        try
        {
            context.Database.ExecuteSqlRaw(sql);
            logger.LogInformation("FavoriteMatches tablosu kontrol edildi / olusturuldu.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "FavoriteMatches tablosu olusturulurken hata olustu, EnsureCreated deneniyor...");
            try
            {
                context.Database.EnsureCreated();
                logger.LogInformation("FavoriteMatches tablosu EnsureCreated ile olusturuldu!");
            }
            catch (Exception ex2)
            {
                logger.LogError(ex2, "FavoriteMatches tablosu olusturulamadi!");
            }
        }
    }
}
