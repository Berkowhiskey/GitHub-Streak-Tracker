using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using StreakTracker.API.Models.Badges;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.API.Controllers;

/// <summary>
/// Profil README'lerine gomulen dinamik SVG rozetlerini sunar.
/// Herkese aciktir ve yalnizca veritabanindan okur; GitHub API'sine gitmez.
/// </summary>
[ApiController]
[Route("api/v1/badges")]
[AllowAnonymous]
public class BadgeController : ControllerBase
{
    /// <summary>
    /// Rozetin tarayici/proxy onbelleginde tutulacagi sure.
    /// Streak gun icinde degisebildigi icin kisa tutulur.
    /// </summary>
    private const int CacheMaxAgeSeconds = 300;

    private readonly IStreakService _streakService;
    private readonly ISvgBadgeService _badgeService;
    private readonly ILogger<BadgeController> _logger;

    public BadgeController(
        IStreakService streakService,
        ISvgBadgeService badgeService,
        ILogger<BadgeController> logger)
    {
        _streakService = streakService;
        _badgeService = badgeService;
        _logger = logger;
    }

    /// <summary>
    /// Kullanicinin streak rozetini SVG olarak dondurur.
    /// Ornek: /api/v1/badges/Berkowhiskey.svg?theme=light
    /// </summary>
    [HttpGet("{username}.svg")]
    [Produces("image/svg+xml")]
    public async Task<IActionResult> GetBadge(
        string username,
        [FromQuery] string? theme,
        CancellationToken cancellationToken)
    {
        var badgeTheme = ParseTheme(theme);

        var data = await _streakService.GetBadgeDataAsync(username, cancellationToken);

        if (data is null)
        {
            _logger.LogInformation("Rozet istendi ama kullanici kayitli degil: {Username}", username);

            // README'de kirik resim cikmamasi icin 200 ile bilgilendirici bir rozet doneriz.
            // Bu icerik onbellege alinmamali; kullanici birazdan kaydolabilir.
            Response.Headers[HeaderNames.CacheControl] = "no-cache, no-store, must-revalidate";

            return Content(_badgeService.GenerateNotFoundBadge(username, badgeTheme), "image/svg+xml");
        }

        var etag = _badgeService.ComputeETag(data, badgeTheme);

        // Streak degismediyse icerigi yeniden gondermeye gerek yok.
        if (Request.Headers.IfNoneMatch.Any(value => value == etag))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers[HeaderNames.ETag] = etag;
        Response.Headers[HeaderNames.CacheControl] = $"public, max-age={CacheMaxAgeSeconds}";

        return Content(_badgeService.GenerateStreakBadge(data, badgeTheme), "image/svg+xml");
    }

    private static BadgeTheme ParseTheme(string? theme) =>
        string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase)
            ? BadgeTheme.Light
            : BadgeTheme.Dark;
}
