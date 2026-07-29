using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StreakTracker.API.Exceptions;

namespace StreakTracker.API.Middleware;

/// <summary>
/// Yakalanmamis istisnalari tek noktada anlamli HTTP yanitlarina cevirir.
/// Boylece controller'lar try-catch ile dolmaz ve hata formatlari tutarli kalir.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            GitHubServiceException { IsRateLimited: true } rateLimited => new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "GitHub istek siniri asildi",
                Detail = rateLimited.RateLimitResetsAt is { } reset
                    ? $"Lutfen {reset.UtcDateTime:HH:mm} UTC sonrasinda tekrar deneyin."
                    : "Lutfen bir sure sonra tekrar deneyin."
            },

            GitHubServiceException gitHubEx => new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway,
                Title = "GitHub servisinde hata",
                Detail = gitHubEx.Message
            },

            ArgumentException argEx => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Gecersiz istek",
                Detail = argEx.Message
            },

            InvalidOperationException invalidEx => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Islem gerceklestirilemedi",
                Detail = invalidEx.Message
            },

            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Beklenmeyen bir hata olustu",
                // Ic hata detaylari istemciye sizdirilmaz; yalnizca loglanir.
                Detail = "Islem tamamlanamadi. Lutfen daha sonra tekrar deneyin."
            }
        };

        _logger.LogError(exception,
            "Islenmemis istisna. Yol: {Path}, Durum: {StatusCode}",
            httpContext.Request.Path, problem.Status);

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
