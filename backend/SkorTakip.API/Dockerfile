# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["SkorTakip.API.csproj", "./"]
RUN dotnet restore "SkorTakip.API.csproj"

COPY . .
RUN dotnet publish "SkorTakip.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

EXPOSE 5000

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SkorTakip.API.dll"]
