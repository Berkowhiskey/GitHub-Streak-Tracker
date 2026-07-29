using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StreakTracker.API.Data;
using StreakTracker.API.Models.Auth;
using StreakTracker.API.Models.Users;
using StreakTracker.API.Options;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.API.Controllers;

[Route("api/v1/users")]
[Authorize]
public class UsersController : BaseApiController
{
    private readonly AppDbContext _dbContext;
    private readonly IAuthService _authService;
    private readonly AppOptions _appOptions;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        AppDbContext dbContext,
        IAuthService authService,
        IOptions<AppOptions> appOptions,
        ILogger<UsersController> logger)
    {
        _dbContext = dbContext;
        _authService = authService;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Giris yapmis kullanicinin profil ozeti.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        var user = await _authService.GetCurrentUserAsync(CurrentUserId, cancellationToken);

        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Bildirim tercihlerini gunceller (bildirim saati, bildirimlerin acik/kapali olmasi).
    /// </summary>
    [HttpPatch("me/preferences")]
    public async Task<ActionResult<CurrentUserDto>> UpdatePreferences(
        [FromBody] UpdatePreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId, cancellationToken);

        if (user is null)
            return NotFound();

        if (request.PreferredNotificationHourUtc is { } hour)
        {
            if (hour is < 0 or > 23)
                throw new ArgumentException("Bildirim saati 0-23 araliginda olmalidir.");

            user.PreferredNotificationHourUtc = hour;
        }

        if (request.IsActive is { } isActive)
        {
            user.IsActive = isActive;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Bildirim tercihleri guncellendi. Kullanici: {Username}, Saat: {Hour}, Aktif: {IsActive}",
            user.GitHubUsername, user.PreferredNotificationHourUtc, user.IsActive);

        var updated = await _authService.GetCurrentUserAsync(CurrentUserId, cancellationToken);

        return Ok(updated);
    }

    /// <summary>
    /// Profil README'sine yapistirilabilecek hazir rozet kodlarini dondurur.
    /// </summary>
    [HttpGet("me/badge")]
    public async Task<ActionResult<BadgeSnippetsDto>> BadgeSnippets(CancellationToken cancellationToken)
    {
        var username = await _dbContext.Users
            .Where(u => u.Id == CurrentUserId)
            .Select(u => u.GitHubUsername)
            .FirstOrDefaultAsync(cancellationToken);

        if (username is null)
            return NotFound();

        var baseUrl = _appOptions.PublicBaseUrl.TrimEnd('/');
        var badgeUrl = $"{baseUrl}/api/v1/badges/{username}.svg";
        var badgeUrlLight = $"{badgeUrl}?theme=light";
        var profileUrl = $"https://github.com/{username}";

        return Ok(new BadgeSnippetsDto(
            BadgeUrl: badgeUrl,
            BadgeUrlLight: badgeUrlLight,
            Markdown: $"[![GitHub Streak]({badgeUrl})]({profileUrl})",
            Html: $"<a href=\"{profileUrl}\"><img src=\"{badgeUrl}\" alt=\"GitHub Streak\" /></a>"));
    }

    /// <summary>
    /// Kullanicinin StreakTracker kaydini ve tum verilerini siler (KVKK silme hakki).
    /// GitHub hesabindaki gizli repo silinmez; ona yalnizca kullanici karar verebilir.
    /// </summary>
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteAccount(CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId, cancellationToken);

        if (user is null)
            return NotFound();

        var username = user.GitHubUsername;
        var repoName = user.PrivateNotificationRepoName;

        // Streak ve bildirim kayitlari cascade delete ile birlikte silinir.
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Kullanici kaydi silindi: {Username}", username);

        return Ok(new
        {
            deleted = true,
            message = repoName is null
                ? "Hesabiniz ve tum verileriniz silindi."
                : $"Hesabiniz ve tum verileriniz silindi. GitHub hesabinizdaki '{repoName}' reposu duruyor; " +
                  "dilerseniz GitHub uzerinden kendiniz silebilirsiniz."
        });
    }
}
