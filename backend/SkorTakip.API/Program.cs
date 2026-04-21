using Microsoft.AspNetCore.HttpOverrides;
using SkorTakip.API.Data;
using SkorTakip.API.Extensions;
using SkorTakip.API.Hubs;
using SkorTakip.API.Infrastructure;
using SkorTakip.API.Middleware;

EnvFileLoader.LoadOptional();

// Render / Heroku / Fly.io gibi platformlar dinlenecek portu $PORT env var ile verir.
// ASPNETCORE_URLS ayarlı değilse bu değeri kullanarak 0.0.0.0:$PORT'u dinle.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port) &&
    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_URLS", $"http://0.0.0.0:{port}");
}

var builder = WebApplication.CreateBuilder(args);

// ==================== SERVİS KAYITLARI ====================
builder.Services.AddControllers();
builder.Services.AddHttpClient("NewsData", client =>
{
    client.Timeout = TimeSpan.FromSeconds(45);
});
builder.Services.AddEndpointsApiExplorer();

// Reverse proxy (nginx / Cloudflare) arkasında doğru IP/scheme okunabilsin
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddSwaggerDocumentation();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddIdentityServices();
builder.Services.AddCorsPolicies(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddSignalRServices(builder.Environment);
builder.Services.AddApiRateLimiting();

// Response compression — public API yanıtlarının boyutunu küçültür
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

// ==================== MİDDLEWARE PİPELINE ====================
app.UseForwardedHeaders();

// Güvenlik başlıkları — nginx ayrıca ekliyor; defense-in-depth olarak burada da var
app.Use(async (ctx, next) =>
{
    var headers = ctx.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    // Server header'ı sızdırmayı azalt
    headers.Remove("Server");
    await next();
});

app.UseResponseCompression();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowVueApp");
app.UseAuthentication();
app.UseAuthorization();
// Rate limiter'ı auth sonrasına aldık ki policy'ler User.Id üzerinden partition edebilsin.
app.UseRateLimiter();

app.MapControllers();
app.MapHub<MatchHub>("/matchhub");

// Basit sağlık kontrolü (docker healthcheck / load balancer için)
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }))
   .AllowAnonymous();

// ==================== VERİTABANI BAŞLATMA ====================
DatabaseInitializer.Initialize(app.Services);
await DatabaseInitializer.SeedAdminAsync(app.Services);

app.Run();
