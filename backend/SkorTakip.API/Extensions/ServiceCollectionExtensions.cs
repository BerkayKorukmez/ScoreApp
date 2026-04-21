using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SkorTakip.API.Data;
using SkorTakip.API.Models;
using SkorTakip.API.Services;
using SkorTakip.API.Services.Interfaces;
using System.Text;

namespace SkorTakip.API.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// PostgreSQL veritabanı bağlantısını yapılandırır.
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        return services;
    }

    /// <summary>
    /// ASP.NET Identity servislerini yapılandırır.
    /// </summary>
    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }

    /// <summary>
    /// JWT tabanlı kimlik doğrulamayı yapılandırır.
    /// Üretimde JWT secret anahtarının en az 32 karakter olması zorunludur.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];

        if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JwtSettings:SecretKey boş veya 32 karakterden kısa. .env / ortam değişkenleri üzerinden güçlü bir JWT_SECRET ayarlayın.");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
                // Varsayılan 5 dk tolerans production için çok geniş — token süresi bittiği an geçersiz olsun.
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            // SignalR WebSocket bağlantılarında token query string'den okunur
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var accessToken = ctx.Request.Query["access_token"];
                    var path = ctx.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/matchhub"))
                        ctx.Token = accessToken;
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// Uygulama servislerini (DI) kayıt eder.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Auth
        services.AddScoped<IJwtService, JwtService>();

        // External API (HttpClientFactory ile yönetiliyor)
        services.AddHttpClient<IExternalApiService, ExternalApiService>();

        // Live match — singleton çünkü cache tutması gerekiyor
        services.AddSingleton<LiveMatchService>();
        services.AddSingleton<ILiveMatchService>(sp => sp.GetRequiredService<LiveMatchService>());
        services.AddHostedService(sp => sp.GetRequiredService<LiveMatchService>());

        // Match simulation — DB'deki canlı maçları simüle eder
        services.AddHostedService<MatchSimulationService>();

        // Sohbet temizleme — maç bitiminden 1 gün sonra yorumları siler
        services.AddHostedService<ChatCleanupService>();

        // AI Chat (Gemini) — sohbet; maç önizlemesi ayrı servis
        services.AddHttpClient<IAiChatService, AiChatService>();
        services.AddHttpClient<IMatchPreviewAiService, MatchPreviewAiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(90);
        });

        return services;
    }

    /// <summary>
    /// CORS politikasını yapılandırır.
    /// İzin verilen origin'ler "AllowedOrigins" config anahtarından okunur (virgülle ayrılmış).
    /// Örn: appsettings.json → "AllowedOrigins": "http://localhost:5173"
    ///      docker-compose   → AllowedOrigins=http://localhost:3000
    /// </summary>
    public static IServiceCollection AddCorsPolicies(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = (configuration.GetValue<string>("AllowedOrigins") ?? "http://localhost:5173")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        services.AddCors(options =>
        {
            options.AddPolicy("AllowVueApp", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });
        return services;
    }

    /// <summary>
    /// Swagger/OpenAPI yapılandırması.
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Skor Takip API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
        return services;
    }

    /// <summary>
    /// SignalR (WebSocket) yapılandırması.
    /// Üretimde detaylı hata mesajları kapatılır (istemciye stack trace gitmesin).
    /// </summary>
    public static IServiceCollection AddSignalRServices(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = environment.IsDevelopment();
            options.MaximumReceiveMessageSize = 32 * 1024; // 32 KB — yorumlar için yeterli, büyük payload'ları engeller
        });
        return services;
    }

    /// <summary>
    /// Temel istek hız sınırlama (auth uçları + genel fallback).
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // 429 yanıtında JSON gövde + Retry-After başlığı. Frontend e.response.data.message üzerinden gösteriyor.
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(
                        System.Threading.RateLimiting.MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
                }

                var path = context.HttpContext.Request.Path.Value ?? string.Empty;
                var message = path.StartsWith("/api/match-preview", StringComparison.OrdinalIgnoreCase)
                    ? "Günlük yapay zeka tahmin hakkınız doldu. Her kullanıcı günde en fazla 3 istek yapabilir."
                    : "Çok sık istek atıldı. Lütfen bir süre sonra tekrar deneyin.";

                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsJsonAsync(new { message }, token);
            };

            // Giriş / kayıt: brute-force koruması — 10 istek/dakika/IP
            options.AddFixedWindowLimiter("auth", opt =>
            {
                opt.PermitLimit = 10;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 0;
            });

            // Yorum ekleme: spam koruması — 20 istek/dakika/IP
            options.AddFixedWindowLimiter("comments", opt =>
            {
                opt.PermitLimit = 20;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 0;
            });

            // AI maç önizlemesi: kullanıcı başına günde 3 istek (Gemini kotası korunsun).
            // [Authorize] olduğundan normalde kullanıcı kimliği mevcuttur; olmazsa IP'ye düşer.
            options.AddPolicy("match-preview", context =>
            {
                var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var partitionKey = !string.IsNullOrEmpty(userId)
                    ? $"user:{userId}"
                    : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

                return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromDays(1),
                        QueueLimit = 0
                    });
            });

            // Genel global fallback — 300 istek/dakika/IP (tarayıcının normal kullanımı kapsamlıca geçer)
            options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        return services;
    }
}
