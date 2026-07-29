using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StreakTracker.API.Data;
using StreakTracker.API.Models.Users;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.API.Controllers;

[Route("api/v1/streaks")]
[Authorize]
public class StreaksController : BaseApiController
{
    /// <summary>Takvim gorunumunde gosterilecek varsayilan gun sayisi (yaklasik 1 yil).</summary>
    private const int DefaultCalendarDays = 364;

    private readonly AppDbContext _dbContext;
    private readonly IStreakService _streakService;
    private readonly IGitHubService _gitHubService;

    public StreaksController(
        AppDbContext dbContext,
        IStreakService streakService,
        IGitHubService gitHubService)
    {
        _dbContext = dbContext;
        _streakService = streakService;
        _gitHubService = gitHubService;
    }

    /// <summary>
    /// Kullanicinin mevcut streak durumunu veritabanindan okur (GitHub'a gidilmez, hizlidir).
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<StreakStatusDto>> Me(CancellationToken cancellationToken)
    {
        var streak = await _streakService.GetStreakAsync(CurrentUserId, cancellationToken);

        if (streak is null)
        {
            // Onboarding tamamlanmadiysa henuz streak kaydi olusmamis olabilir.
            return Ok(new StreakStatusDto(0, 0, false, null, DateTime.UtcNow));
        }

        return Ok(new StreakStatusDto(
            streak.CurrentStreak,
            streak.LongestStreak,
            streak.HasCommittedToday,
            streak.LastCommitDate,
            streak.LastCheckedAt));
    }

    /// <summary>
    /// Streak verisini GitHub'dan yeniden hesaplar. Kullanici "yenile" dedigi zaman kullanilir.
    /// </summary>
    [HttpPost("me/refresh")]
    public async Task<ActionResult<StreakStatusDto>> Refresh(CancellationToken cancellationToken)
    {
        var streak = await _streakService.UpdateUserStreakAsync(CurrentUserId, cancellationToken);

        return Ok(new StreakStatusDto(
            streak.CurrentStreak,
            streak.LongestStreak,
            streak.HasCommittedToday,
            streak.LastCommitDate,
            streak.LastCheckedAt));
    }

    /// <summary>
    /// Heatmap gorunumu icin gunluk katki takvimini dondurur.
    /// </summary>
    /// <param name="days">Kac gun geriye gidilecegi (1-364).</param>
    [HttpGet("me/calendar")]
    public async Task<ActionResult<IReadOnlyList<CalendarDayDto>>> Calendar(
        [FromQuery] int days = DefaultCalendarDays,
        CancellationToken cancellationToken = default)
    {
        if (days is < 1 or > DefaultCalendarDays)
            throw new ArgumentException($"Gun sayisi 1-{DefaultCalendarDays} araliginda olmalidir.");

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == CurrentUserId, cancellationToken);

        if (user is null)
            return NotFound();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var contributions = await _gitHubService.GetContributionDaysAsync(
            user.AccessToken,
            user.GitHubUsername,
            today.AddDays(-days),
            today,
            cancellationToken);

        var calendar = contributions
            .Select(c => new CalendarDayDto(c.Date, c.ContributionCount))
            .ToList();

        return Ok(calendar);
    }
}
