using Microsoft.EntityFrameworkCore;
using StreakTracker.API.Data;
using StreakTracker.API.Entities;
using StreakTracker.API.Enums;
using StreakTracker.API.Models.Notifications;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.API.Services;

/// <inheritdoc cref="INotificationService" />
public class NotificationService : INotificationService
{
    private readonly AppDbContext _dbContext;
    private readonly IGitHubAppService _gitHubAppService;
    private readonly IStreakService _streakService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext dbContext,
        IGitHubAppService gitHubAppService,
        IStreakService streakService,
        ILogger<NotificationService> logger)
    {
        _dbContext = dbContext;
        _gitHubAppService = gitHubAppService;
        _streakService = streakService;
        _logger = logger;
    }

    public async Task<HourlyNotificationSummary> ProcessHourlyNotificationsAsync(
        int utcHour,
        CancellationToken cancellationToken = default)
    {
        var userIds = await _dbContext.Users
            .Where(u => u.IsActive
                        && u.HasAcceptedTerms
                        && u.NotificationIssueNumber != null
                        && u.PreferredNotificationHourUtc == utcHour)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Saatlik bildirim turu basladi. Saat: {Hour}:00 UTC, Kullanici sayisi: {Count}",
            utcHour, userIds.Count);

        var sent = 0;
        var failures = 0;

        foreach (var userId in userIds)
        {
            // Bir kullanicida olusan hata (gecersiz token, rate-limit, silinmis repo)
            // turun geri kalanini durdurmamalidir.
            try
            {
                var result = await SendStreakWarningAsync(userId, cancellationToken);

                if (result.Sent)
                    sent++;
            }
            catch (Exception ex)
            {
                failures++;
                _logger.LogError(ex, "Kullanici islenirken hata olustu. UserId: {UserId}", userId);
            }
        }

        _logger.LogInformation(
            "Saatlik bildirim turu bitti. Saat: {Hour}:00 UTC, Gonderilen: {Sent}, Hata: {Failures}",
            utcHour, sent, failures);

        return new HourlyNotificationSummary(utcHour, userIds.Count, sent, failures);
    }

    public async Task<NotificationResult> SendStreakWarningAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            throw new InvalidOperationException($"Kullanici bulunamadi: {userId}");

        if (!IsReadyForNotifications(user, out var notReadyReason))
            return NotificationResult.Skipped(notReadyReason);

        // Karar vermeden once GitHub'daki guncel duruma bakariz;
        // kullanici bildirim saatinden hemen once commit atmis olabilir.
        var streak = await _streakService.UpdateUserStreakAsync(user.Id, cancellationToken);

        if (streak.HasCommittedToday)
        {
            _logger.LogInformation(
                "{Username} bugun commit atmis, bildirim gonderilmedi.", user.GitHubUsername);

            return NotificationResult.Skipped("Bugun commit atilmis, serin guvende.");
        }

        if (await HasBeenNotifiedTodayAsync(user.Id, cancellationToken))
        {
            _logger.LogInformation(
                "{Username} icin bugun zaten bildirim gonderilmis, tekrar gonderilmedi.", user.GitHubUsername);

            return NotificationResult.Skipped("Bugun zaten bildirim gonderilmis.");
        }

        var hoursLeft = 24 - DateTime.UtcNow.Hour;

        var message = NotificationMessageBuilder.BuildStreakWarning(
            user.GitHubUsername, streak.CurrentStreak, streak.LongestStreak, hoursLeft);

        return await DispatchAsync(user, message, isTest: false, cancellationToken);
    }

    public async Task<NotificationResult> SendTestNotificationAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            throw new InvalidOperationException($"Kullanici bulunamadi: {userId}");

        if (!IsReadyForNotifications(user, out var notReadyReason))
            return NotificationResult.Skipped(notReadyReason);

        // Test bildiriminde streak'i tazeleriz ki mesajdaki rakam gercek olsun,
        // ancak "bugun commit atilmis mi" kontrolu uygulanmaz - amac kurulumu dogrulamak.
        var streak = await _streakService.UpdateUserStreakAsync(user.Id, cancellationToken);

        var message = NotificationMessageBuilder.BuildTestNotification(
            user.GitHubUsername, streak.CurrentStreak, streak.HasCommittedToday);

        return await DispatchAsync(user, message, isTest: true, cancellationToken);
    }

    // -----------------------------------------------------------------------
    // Yardimcilar
    // -----------------------------------------------------------------------

    /// <summary>
    /// Kullanicinin bildirim alabilecek durumda olup olmadigini kontrol eder.
    /// </summary>
    private static bool IsReadyForNotifications(User user, out string reason)
    {
        if (!user.IsActive)
        {
            reason = "Bildirimler kapali.";
            return false;
        }

        if (!user.HasAcceptedTerms)
        {
            reason = "Bildirim onayi verilmemis.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(user.PrivateNotificationRepoName) || user.NotificationIssueNumber is null)
        {
            reason = "Bildirim altyapisi kurulmamis. Once onboarding tamamlanmali.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    /// <summary>
    /// Bugun (UTC) bu kullaniciya basarili bir gercek bildirim gonderilmis mi?
    /// Test bildirimleri sayilmaz; test gondermek gunun gercek uyarisini engellememelidir.
    /// </summary>
    private Task<bool> HasBeenNotifiedTodayAsync(Guid userId, CancellationToken cancellationToken)
    {
        var todayStart = DateTime.UtcNow.Date;

        return _dbContext.NotificationLogs.AnyAsync(
            n => n.UserId == userId
                 && n.IsSuccess
                 && !n.IsTest
                 && n.SentAt >= todayStart,
            cancellationToken);
    }

    /// <summary>
    /// Kullanicinin GitHub App kurulum kimligini dondurur.
    /// Veritabaninda yoksa GitHub'a sorar ve sonucu kaydeder.
    /// </summary>
    private async Task<long?> ResolveInstallationIdAsync(User user, CancellationToken cancellationToken)
    {
        if (user.GitHubAppInstallationId is { } cached)
            return cached;

        var installationId = await _gitHubAppService.GetInstallationIdAsync(
            user.GitHubUsername, cancellationToken);

        if (installationId is not null)
        {
            user.GitHubAppInstallationId = installationId;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return installationId;
    }

    /// <summary>
    /// Bildirimi GitHub'a gonderir ve sonucu - basarili da olsa basarisiz da olsa - loglar.
    /// Yorum, kullanicinin kendisi yerine <b>bot kimligiyle</b> atilir; aksi halde
    /// GitHub "kendi eylemin" sayar ve telefona push bildirimi dusmez.
    /// </summary>
    private async Task<NotificationResult> DispatchAsync(
        User user,
        string message,
        bool isTest,
        CancellationToken cancellationToken)
    {
        if (!_gitHubAppService.IsConfigured)
        {
            return NotificationResult.Skipped(
                "GitHub App yapilandirilmamis. Bildirimler bot kimligi olmadan gonderilemez.");
        }

        var installationId = await ResolveInstallationIdAsync(user, cancellationToken);

        if (installationId is null)
        {
            _logger.LogInformation(
                "{Username} GitHub App'i kurmamis; bildirim gonderilemedi.", user.GitHubUsername);

            return NotificationResult.Skipped(
                "Bildirimlerin telefonuna dusebilmesi icin GitHub App'i kurman gerekiyor.");
        }

        var log = new NotificationLog
        {
            UserId = user.Id,
            Channel = NotificationChannel.GitHubIssue,
            Message = message,
            IsTest = isTest,
            SentAt = DateTime.UtcNow
        };

        try
        {
            await _gitHubAppService.SendNotificationCommentAsync(
                installationId.Value,
                user.GitHubUsername,
                user.PrivateNotificationRepoName!,
                user.NotificationIssueNumber!.Value,
                message,
                cancellationToken);

            log.IsSuccess = true;

            _logger.LogInformation(
                "Bildirim gonderildi. Kullanici: {Username}, Test: {IsTest}", user.GitHubUsername, isTest);
        }
        catch (Exception ex)
        {
            log.IsSuccess = false;
            log.ErrorMessage = Truncate(ex.Message, 2000);

            _logger.LogError(ex,
                "Bildirim gonderilemedi. Kullanici: {Username}, Test: {IsTest}", user.GitHubUsername, isTest);
        }
        finally
        {
            // Basarisiz denemeler de kayda gecmeli; sorunlari geriye donuk inceleyebilmek icin.
            _dbContext.NotificationLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return log.IsSuccess
            ? NotificationResult.Success(isTest
                ? "Test bildirimi gonderildi. Telefonuna dusmesi birkac saniye surebilir."
                : "Bildirim gonderildi.")
            : NotificationResult.Skipped($"Bildirim gonderilemedi: {log.ErrorMessage}");
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
