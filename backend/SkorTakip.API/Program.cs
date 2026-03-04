using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SkorTakip.API.Data;
using SkorTakip.API.Hubs;
using SkorTakip.API.Interfaces;
using SkorTakip.API.Models;
using SkorTakip.API.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
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

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
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

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
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
});

builder.Services.AddAuthorization();

// Services
builder.Services.AddScoped<JwtService>();
builder.Services.AddHttpClient<IExternalApiService, ExternalApiService>();
builder.Services.AddScoped<IExternalApiService, ExternalApiService>();

// LiveMatchService: API'den periyodik veri çekip WebSocket ile yayınlar
// Singleton olmalı çünkü cache tutması gerekiyor
builder.Services.AddSingleton<LiveMatchService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<LiveMatchService>());

// MatchSimulationService: DB'deki canlı maçları simüle eder
builder.Services.AddHostedService<MatchSimulationService>();

// SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowVueApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MatchHub>("/matchhub");

// Database initialization (development only)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Veritabani hazirlaniyor...");
        
        // Önce bağlantıyı kontrol et
        if (context.Database.CanConnect())
        {
            // Veritabanı var, tabloları kontrol et
            try
            {
                // Matches tablosunu sorgula - yoksa hata verir
                var test = context.Matches.Count();
                logger.LogInformation("Veritabani hazir, tablolar mevcut.");
            }
            catch
            {
                // Tablolar yok, oluştur
                logger.LogInformation("Tablolar bulunamadi, olusturuluyor...");
                context.Database.EnsureCreated();
                logger.LogInformation("Tablolar olusturuldu!");
            }
        }
        else
        {
            // Veritabanı yok, oluştur
            logger.LogInformation("Veritabani olusturuluyor...");
            context.Database.EnsureCreated();
            logger.LogInformation("Veritabani olusturuldu!");
        }

        // Matches tablosunda SportType kolonu yoksa ekle
        logger.LogInformation("Matches tablosu SportType kolonu kontrol ediliyor...");
        const string addSportTypeColumnSql = @"
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

        context.Database.ExecuteSqlRaw(addSportTypeColumnSql);
        logger.LogInformation("Matches.SportType kolonu kontrol edildi / eklendi.");

        // FavoriteMatches tablosunun var olduğundan emin ol
        logger.LogInformation("FavoriteMatches tablosu kontrol ediliyor...");
        const string createFavoriteMatchesTableSql = @"
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
            context.Database.ExecuteSqlRaw(createFavoriteMatchesTableSql);
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
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database initialization hatasi: {Message}", ex.Message);
    }
}

app.Run();
