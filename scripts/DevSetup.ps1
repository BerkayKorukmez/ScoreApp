# Tek seferlik / günlük: PostgreSQL konteyneri + EF migration (kök dizinden çalıştırın)
# Örnek:  pwsh -File scripts/DevSetup.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if (-not (Test-Path (Join-Path $root ".env"))) {
    Write-Host ".env bulunamadi. Ornek: Copy-Item .env.example .env" -ForegroundColor Yellow
    exit 1
}

Write-Host "PostgreSQL (docker compose) baslatiliyor..." -ForegroundColor Cyan
docker compose up -d postgres
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$max = 30
for ($i = 0; $i -lt $max; $i++) {
    $status = docker inspect -f "{{.State.Health.Status}}" skortakip-postgres 2>$null
    if ($status -eq "healthy") { break }
    Start-Sleep -Seconds 2
}

Write-Host "EF database update..." -ForegroundColor Cyan
Set-Location (Join-Path $root "backend\SkorTakip.API")
dotnet ef database update
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Hata: Veritabani zaten elle olusturulduysa ve __EFMigrationsHistory bossa," -ForegroundColor Yellow
    Write-Host "  backend\SkorTakip.API\scripts\baseline-ef-migrations.sql dosyasini PostgreSQL'e uygulayip tekrar deneyin." -ForegroundColor Yellow
    exit $LASTEXITCODE
}

Write-Host "Tamam." -ForegroundColor Green
