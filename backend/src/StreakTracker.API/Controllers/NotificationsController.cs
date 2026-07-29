using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreakTracker.API.Models.Notifications;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.API.Controllers;

[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Kullaniciya test bildirimi gonderir; kurulumun (gizli repo, Issue, GitHub Mobile)
    /// gercekten calistigini dogrulamak icindir. Saat ve mukerrerlik kontrolu uygulanmaz.
    /// </summary>
    [HttpPost("test")]
    public async Task<ActionResult<NotificationResult>> SendTest(CancellationToken cancellationToken)
    {
        var result = await _notificationService.SendTestNotificationAsync(CurrentUserId, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Kullanicinin streak uyarisini simdi degerlendirir: bugun commit atilmamissa
    /// ve bugun daha once bildirim gonderilmemisse gercek uyariyi gonderir.
    /// Zamanlanmis turu beklemeden akisi denemek icin kullanilir.
    /// </summary>
    [HttpPost("check-now")]
    public async Task<ActionResult<NotificationResult>> CheckNow(CancellationToken cancellationToken)
    {
        var result = await _notificationService.SendStreakWarningAsync(CurrentUserId, cancellationToken);

        return Ok(result);
    }
}
