namespace SkorTakip.API.Infrastructure;

/// <summary>
/// Proje kökündeki .env dosyasını okuyup ortam değişkenlerine yazar (WebApplication.CreateBuilder öncesi).
/// Böylece <c>dotnet run</c> ile çalışırken appsettings'teki placeholder'lar yerine .env kullanılır.
/// </summary>
public static class EnvFileLoader
{
    public static void LoadOptional()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var path = FindEnvFilePath();
        if (path != null && File.Exists(path))
        {
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
        }

        // .env dosyasında olmayan ama sistemde set edilmiş değişkenler (Render/Docker/CI) da okunabilmeli.
        string? G(string k)
        {
            if (dict.TryGetValue(k, out var v) && !string.IsNullOrEmpty(v))
                return v;
            var env = Environment.GetEnvironmentVariable(k);
            return string.IsNullOrEmpty(env) ? null : env;
        }

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
        SetIfEmpty("Gemini__ApiKey", G("GEMINI_API_KEY"));
        SetIfEmpty("Gemini__Model", G("GEMINI_MODEL"));
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
        // 1) Render / Heroku tarzı tek parça URL: postgres://user:pass@host:port/db[?sslmode=require]
        var url = g("DATABASE_URL") ?? g("POSTGRES_URL");
        if (!string.IsNullOrEmpty(url))
        {
            var parsed = ParsePostgresUrl(url);
            if (parsed != null)
                return parsed;
        }

        // 2) Klasik ayrı değişkenler
        var user = g("POSTGRES_USER");
        var pass = g("POSTGRES_PASSWORD");
        var db = g("POSTGRES_DB");
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(db))
            return null;
        var host = g("POSTGRES_HOST") ?? "localhost";
        var port = g("POSTGRES_PORT") ?? "5432";
        pass ??= "";

        var baseConn = $"Host={host};Port={port};Database={db};Username={user};Password={pass}";

        // localhost dışında bir host varsa (managed DB: Render/Supabase/Neon/vb.) SSL gerekli.
        if (!IsLocalHost(host))
            baseConn += ";SSL Mode=Require;Trust Server Certificate=true";

        return baseConn;
    }

    private static string? ParsePostgresUrl(string url)
    {
        try
        {
            // Npgsql "postgres://" şemasını doğrudan anlamaz; key-value formatına çeviriyoruz.
            var uri = new Uri(url.Replace("postgres://", "postgresql://", StringComparison.OrdinalIgnoreCase));
            if (!uri.Scheme.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
                return null;

            var userInfo = uri.UserInfo.Split(':', 2);
            var user = Uri.UnescapeDataString(userInfo[0]);
            var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var db = uri.AbsolutePath.TrimStart('/');
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(db))
                return null;

            var conn = $"Host={host};Port={port};Database={db};Username={user};Password={pass}";
            var sslNeeded = !IsLocalHost(host) ||
                            url.Contains("sslmode=require", StringComparison.OrdinalIgnoreCase);
            if (sslNeeded)
                conn += ";SSL Mode=Require;Trust Server Certificate=true";
            return conn;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsLocalHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        host == "127.0.0.1" ||
        host == "::1" ||
        host.Equals("postgres", StringComparison.OrdinalIgnoreCase); // docker-compose servis adı

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
