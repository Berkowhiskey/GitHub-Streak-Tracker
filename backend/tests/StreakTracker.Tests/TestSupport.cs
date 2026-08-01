using Microsoft.EntityFrameworkCore;
using StreakTracker.API.Data;
using StreakTracker.API.Entities;
using StreakTracker.API.Enums;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.Tests;

/// <summary>
/// Bagimliligi olan servisleri test edebilmek icin ortak yardimcilar.
/// Bu ana kadar yalnizca saf siniflar (StreakCalculator, UserClock,
/// NotificationMessageBuilder, SvgBadgeService) test ediliyordu; asil hatalar ise
/// bagimliligi olan katmanda cikti.
/// </summary>
internal static class TestSupport
{
    /// <summary>
    /// Her testin kendi izole veritabani olur; testler birbirinin verisini gormez.
    /// </summary>
    public static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"streaktracker-tests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options, new PassThroughTokenProtector());
    }

    /// <summary>
    /// Bildirim gonderilebilir durumda, varsayilan ayarlara sahip bir kullanici uretir.
    /// Testler yalnizca ilgilendikleri alani degistirir.
    /// </summary>
    public static User CreateUser(
        string username = "testuser",
        string timeZoneId = "UTC",
        int preferredHour = 20,
        AppLanguage language = AppLanguage.Turkish,
        bool isActive = true,
        bool hasAcceptedTerms = true,
        int? notificationIssueNumber = 1,
        long? appInstallationId = 12345)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            GitHubId = Random.Shared.Next(1, int.MaxValue),
            GitHubUsername = username,
            AccessToken = "gho_test_token",
            TimeZoneId = timeZoneId,
            PreferredNotificationHour = preferredHour,
            Language = language,
            IsActive = isActive,
            HasAcceptedTerms = hasAcceptedTerms,
            PrivateNotificationRepoName = ".streak-tracker-notifications",
            NotificationIssueNumber = notificationIssueNumber,
            GitHubAppInstallationId = appInstallationId,
        };
    }
}

/// <summary>
/// Testlerde sifreleme yapmayan koruyucu: DataProtection altyapisini ayaga
/// kaldirmadan DbContext olusturulabilsin diye.
/// </summary>
internal sealed class PassThroughTokenProtector : ITokenProtector
{
    public string Protect(string plainText) => plainText;

    public string Unprotect(string protectedText) => protectedText;

    public bool IsProtected(string value) => false;
}
