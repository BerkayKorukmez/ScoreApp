using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];

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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
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
    /// </summary>
    public static IServiceCollection AddSignalRServices(this IServiceCollection services)
    {
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        });
        return services;
    }
}
