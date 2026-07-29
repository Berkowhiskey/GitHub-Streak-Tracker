using Microsoft.EntityFrameworkCore;
using StreakTracker.API.Data;
using StreakTracker.API.Entities;
using StreakTracker.API.Models.Badges;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.API.Services;

/// <inheritdoc cref="IStreakService" />
public class StreakService : IStreakService
{
    /// <summary>
    /// GitHub'dan cekilecek katki penceresi. contributionsCollection en fazla 1 yillik
    /// aralik kabul eder; tek sorguda tum seriyi yeniden hesaplayabilmek icin ust siniri kullaniriz.
    /// </summary>
    private const int ContributionWindowDays = 364;

    private readonly AppDbContext _dbContext;
    private readonly IGitHubService _gitHubService;
    private readonly ILogger<StreakService> _logger;

    public StreakService(
        AppDbContext dbContext,
        IGitHubService gitHubService,
        ILogger<StreakService> logger)
    {
        _dbContext = dbContext;
        _gitHubService = gitHubService;
        _logger = logger;
    }

    public async Task<Streak> UpdateUserStreakAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.Streak)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException($"Kullanici bulunamadi: {userId}");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-ContributionWindowDays);

        var contributionDays = await _gitHubService.GetContributionDaysAsync(
            user.AccessToken,
            user.GitHubUsername,
            from,
            today,
            cancellationToken);

        var result = StreakCalculator.Calculate(contributionDays, today);

        var streak = user.Streak;

        if (streak is null)
        {
            streak = new Streak { UserId = user.Id };
            _dbContext.Streaks.Add(streak);
        }

        streak.CurrentStreak = result.CurrentStreak;

        // Rekor yalnizca buyudugunde guncellenir: cekilen 1 yillik pencere,
        // daha eski bir rekoru gormezden gelebilir; mevcut kaydi asla dusurmeyiz.
        streak.LongestStreak = Math.Max(streak.LongestStreak, result.LongestStreak);

        streak.LastCommitDate = result.LastCommitDate;
        streak.HasCommittedToday = result.HasCommittedToday;
        streak.LastCheckedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Streak guncellendi. Kullanici: {Username}, Guncel seri: {Current}, Rekor: {Longest}, Bugun commit: {Today}",
            user.GitHubUsername, streak.CurrentStreak, streak.LongestStreak, streak.HasCommittedToday);

        return streak;
    }

    public Task<Streak?> GetStreakAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Streaks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
    }

    public async Task<BadgeData?> GetBadgeDataAsync(string username, CancellationToken cancellationToken = default)
    {
        // GitHub kullanici adlari buyuk/kucuk harf duyarsizdir; rozet adresi de oyle davranmali.
        var normalized = username.Trim().ToLowerInvariant();

        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.GitHubUsername.ToLower() == normalized)
            .Select(u => new BadgeData(
                u.GitHubUsername,
                u.Streak != null ? u.Streak.CurrentStreak : 0,
                u.Streak != null ? u.Streak.LongestStreak : 0,
                u.Streak != null && u.Streak.HasCommittedToday,
                u.Streak != null ? u.Streak.LastCommitDate : null))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
