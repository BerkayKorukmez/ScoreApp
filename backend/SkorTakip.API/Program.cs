using SkorTakip.API.Data;
using SkorTakip.API.Extensions;
using SkorTakip.API.Hubs;
using SkorTakip.API.Infrastructure;
using SkorTakip.API.Middleware;

EnvFileLoader.LoadOptional();

var builder = WebApplication.CreateBuilder(args);

// ==================== SERVİS KAYITLARI ====================
builder.Services.AddControllers();
builder.Services.AddHttpClient("NewsData", client =>
{
    client.Timeout = TimeSpan.FromSeconds(45);
});
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerDocumentation();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddIdentityServices();
builder.Services.AddCorsPolicies(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddSignalRServices();

var app = builder.Build();

// ==================== MİDDLEWARE PİPELINE ====================
app.UseMiddleware<ExceptionHandlingMiddleware>();

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

// ==================== VERİTABANI BAŞLATMA ====================
DatabaseInitializer.Initialize(app.Services);
await DatabaseInitializer.SeedAdminAsync(app.Services);

app.Run();
