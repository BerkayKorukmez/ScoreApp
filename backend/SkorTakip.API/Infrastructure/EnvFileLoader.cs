namespace SkorTakip.API.Infrastructure;

/// <summary>
/// Proje kökündeki .env dosyasını okuyup ortam değişkenlerine yazar (WebApplication.CreateBuilder öncesi).
/// Böylece <c>dotnet run</c> ile çalışırken appsettings'teki placeholder'lar yerine .env kullanılır.
/// </summary>
public static class EnvFileLoader
{
    public static void LoadOptional()
    {
        var path = FindEnvFilePath();
        if (path == null || !File.Exists(path))
            return;

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                continue;
            var eq = line.IndexOf('=');
            if (eq <= 0)
                continue;
            var key = line[..eq].Trim();
            var val = line[(eq + 1)..].Trim();
            if (val.Length >= 2 && val[0] == '"' && val[^1] == '"')
                val = val[1..^1];
            if (key.Length > 0)
                dict[key] = val;
        }

        string? G(string k) => dict.TryGetValue(k, out var v) ? v : null;

        void SetIfEmpty(string envKey, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envKey)))
                return;
            Environment.SetEnvironmentVariable(envKey, value);
        }

        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")))
        {
            var conn = BuildPostgresConnectionString(G);
            if (!string.IsNullOrEmpty(conn))
                Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", conn);
        }

        SetIfEmpty("JwtSettings__SecretKey", G("JWT_SECRET"));
        SetIfEmpty("ApiSports__FootballApiKey", G("FOOTBALL_API_KEY"));
        SetIfEmpty("ApiSports__BasketballApiKey", G("BASKETBALL_API_KEY"));
        SetIfEmpty("ApiSports__VolleyballApiKey", G("VOLLEYBALL_API_KEY"));
        var tennisKey = G("TENNIS_API_KEY");
        if (string.IsNullOrWhiteSpace(tennisKey))
            tennisKey = G("FOOTBALL_API_KEY");
        SetIfEmpty("ApiSports__TennisApiKey", tennisKey);
        SetIfEmpty("CollectApi__ApiKey", G("COLLECT_API_KEY"));
        SetIfEmpty("SportCollectApi__ApiKey", G("SPORT_COLLECT_API_KEY"));
        SetIfEmpty("Gemini__ApiKey", G("GEMINI_API_KEY"));
        SetIfEmpty("Gemini__Model", G("GEMINI_MODEL"));
        var matchPreviewKey = G("MATCH_PREVIEW_GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(matchPreviewKey))
            matchPreviewKey = G("GEMINI_API_KEY");
        SetIfEmpty("MatchPreview__GeminiApiKey", matchPreviewKey);
        SetIfEmpty("MatchPreview__Model", G("MATCH_PREVIEW_GEMINI_MODEL"));
        var newsKey = G("NEWS_API_KEY");
        if (string.IsNullOrWhiteSpace(newsKey))
            newsKey = G("VITE_NEWS_API_KEY");
        SetIfEmpty("NewsData__ApiKey", newsKey);
        SetIfEmpty("Admin__Email", G("ADMIN_EMAIL"));
        SetIfEmpty("Admin__UserName", G("ADMIN_USERNAME"));
        SetIfEmpty("Admin__Password", G("ADMIN_PASSWORD"));
        SetIfEmpty("AllowedOrigins", G("ALLOWED_ORIGINS"));
    }

    private static string? BuildPostgresConnectionString(Func<string, string?> g)
    {
        var user = g("POSTGRES_USER");
        var pass = g("POSTGRES_PASSWORD");
        var db = g("POSTGRES_DB");
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(db))
            return null;
        var host = g("POSTGRES_HOST") ?? "localhost";
        var port = g("POSTGRES_PORT") ?? "5432";
        pass ??= "";
        return $"Host={host};Port={port};Database={db};Username={user};Password={pass}";
    }

    private static string? FindEnvFilePath()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            try
            {
                var dir = new DirectoryInfo(start);
                while (dir != null)
                {
                    var p = Path.Combine(dir.FullName, ".env");
                    if (File.Exists(p))
                        return p;
                    dir = dir.Parent;
                }
            }
            catch
            {
                // ignored
            }
        }

        return null;
    }
}
