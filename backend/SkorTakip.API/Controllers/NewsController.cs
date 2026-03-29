using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace SkorTakip.API.Controllers;

/// <summary>
/// NewsData.io çağrıları sunucu üzerinden yapılır; API anahtarı tarayıcıya gitmez.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class NewsController : ControllerBase
{
    private const string NewsDataBaseUrl = "https://newsdata.io/api/1/news";
    private const string DefaultSportsQuery =
        "futbol OR basketbol OR voleybol OR football OR basketball OR volleyball";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NewsController> _logger;

    public NewsController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<NewsController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Spor haberleri (NewsData.io proxy).
    /// </summary>
    [HttpGet("sports")]
    public async Task<IActionResult> GetSportsNews(
        [FromQuery] string language = "tr",
        [FromQuery] string? q = null,
        [FromQuery] string? page = null,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["NewsData:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Problem(
                detail: "NewsData:ApiKey yapılandırılmamış (.env içinde NEWS_API_KEY veya VITE_NEWS_API_KEY).",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var query = string.IsNullOrWhiteSpace(q) ? DefaultSportsQuery : q.Trim();

        var queryParams = new Dictionary<string, string?>
        {
            ["apikey"] = apiKey,
            ["category"] = "sports",
            ["language"] = language,
            ["q"] = query
        };

        if (!string.IsNullOrWhiteSpace(page))
            queryParams["page"] = page;

        var url = QueryHelpers.AddQueryString(NewsDataBaseUrl, queryParams!);

        var client = _httpClientFactory.CreateClient("NewsData");
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "NewsData.io yanıtı {Status}: {Preview}",
                (int)response.StatusCode,
                body.Length > 400 ? body[..400] : body);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return Problem(
                    title: "Haber API anahtarı geçersiz",
                    detail: "newsdata.io panelinden geçerli bir anahtar alıp .env dosyasındaki NEWS_API_KEY değerini güncelleyin.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            return StatusCode((int)response.StatusCode, body);
        }

        return Content(body, "application/json");
    }
}
