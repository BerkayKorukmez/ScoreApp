namespace SkorTakip.API.Constants;

/// <summary>
/// CollectAPI <c>football/results</c> ile uyumlu lig anahtarları (frontend <c>sports.js</c> ile eşleşir).
/// </summary>
public static class CollectFootballLeagues
{
    public sealed record Entry(
        string CollectKey,
        string LeagueName,
        string Country,
        int? ExternalLeagueId,
        string? LeagueFlag);

    /// <summary>Tarih bazlı geçmiş maçlar için tüm CollectAPI ligleri.</summary>
    public static readonly Entry[] ForHistory =
    [
        new("super-lig", "Süper Lig", "Turkey", 203, "https://media.api-sports.io/flags/tr.svg"),
        new("tff-1-lig", "1. Lig", "Turkey", 204, "https://media.api-sports.io/flags/tr.svg"),
        new("ingiltere-premier-ligi", "Premier League", "England", 39, "https://media.api-sports.io/flags/gb.svg"),
        new("ingiltere-sampiyonluk-ligi", "Championship", "England", 40, "https://media.api-sports.io/flags/gb.svg"),
        new("ispanya-la-liga", "La Liga", "Spain", 140, "https://media.api-sports.io/flags/es.svg"),
        new("italya-serie-a-ligi", "Serie A", "Italy", 135, "https://media.api-sports.io/flags/it.svg"),
        new("almanya-bundesliga", "Bundesliga", "Germany", 78, "https://media.api-sports.io/flags/de.svg"),
        new("fransa-ligue-1", "Ligue 1", "France", 61, "https://media.api-sports.io/flags/fr.svg"),
        new("almanya-bundesliga-2-ligi", "2. Bundesliga", "Germany", 79, "https://media.api-sports.io/flags/de.svg"),
        new("fransa-ligue-2", "Ligue 2", "France", 62, "https://media.api-sports.io/flags/fr.svg"),
        new("sampiyonlar-ligi", "UEFA Champions League", "World", 2, "https://media.api-sports.io/football/leagues/2.png"),
        new("uefa-avrupa-ligi", "UEFA Europa League", "World", 3, "https://media.api-sports.io/football/leagues/3.png"),
        new("uefa-konferans-ligi", "UEFA Europa Conference League", "World", 848, "https://media.api-sports.io/football/leagues/848.png"),
    ];
}
