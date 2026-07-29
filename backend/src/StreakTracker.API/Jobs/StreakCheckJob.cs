using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.API.Jobs;

/// <summary>
/// Her saat basi calisan Hangfire gorevi.
/// Icinde bulunulan UTC saatini bildirim saati olarak secmis kullanicilari isler;
/// boylece her kullanici kendi tercih ettigi saatte uyarilir.
/// </summary>
public class StreakCheckJob
{
    public const string RecurringJobId = "streak-check-hourly";

    /// <summary>Her saatin basinda (UTC) calisir.</summary>
    public const string CronExpression = "0 * * * *";

    private readonly INotificationService _notificationService;
    private readonly ILogger<StreakCheckJob> _logger;

    public StreakCheckJob(INotificationService notificationService, ILogger<StreakCheckJob> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var utcHour = DateTime.UtcNow.Hour;

        _logger.LogInformation("StreakCheckJob tetiklendi. Saat: {Hour}:00 UTC", utcHour);

        var summary = await _notificationService.ProcessHourlyNotificationsAsync(utcHour, cancellationToken);

        _logger.LogInformation(
            "StreakCheckJob tamamlandi. Kontrol edilen: {Checked}, Gonderilen: {Sent}, Hata: {Failures}",
            summary.UsersChecked, summary.NotificationsSent, summary.Failures);
    }
}
