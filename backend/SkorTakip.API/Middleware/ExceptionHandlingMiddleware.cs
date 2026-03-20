namespace SkorTakip.API.Middleware;

/// <summary>
/// Global hata yakalama middleware'i.
/// İşlenmeyen tüm exception'ları yakalar ve tutarlı JSON hata yanıtı döner.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlenmemiş bir hata oluştu: {Method} {Path} — {Message}",
                context.Request.Method, context.Request.Path, ex.Message);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = exception switch
        {
            ArgumentException       => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            KeyNotFoundException    => StatusCodes.Status404NotFound,
            _                       => StatusCodes.Status500InternalServerError
        };

        var isServerError = context.Response.StatusCode == StatusCodes.Status500InternalServerError;
        var response = new
        {
            status  = context.Response.StatusCode,
            message = isServerError
                ? "Sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin."
                : exception.Message
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
