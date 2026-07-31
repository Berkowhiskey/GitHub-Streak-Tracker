using StreakTracker.API.Enums;

namespace StreakTracker.API.Entities;

/// <summary>
/// Gonderilen (veya gonderilmeye calisilan) her bildirimin kaydi.
/// Ayni gun icinde mukerrer bildirim gonderilmesini engellemek ve
/// GitHub API hatalarini geriye donuk incelemek icin kullanilir.
/// </summary>
public class NotificationLog
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Bildirimin gonderildigi kanal (GitHub Issue yorumu, Telegram, E-posta).</summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>Kullaniciya iletilen bildirim metni.</summary>
    public string Message { get; set; } = null!;

    public bool IsSuccess { get; set; }

    /// <summary>
    /// Kullanicinin elle tetikledigi test bildirimi ise true olur.
    /// Mukerrer bildirim kontrolu test kayitlarini saymaz; boylece test gondermek
    /// o gunun gercek uyarisini engellemez.
    /// </summary>
    public bool IsTest { get; set; }

    /// <summary>
    /// Bu kayit bir kilometre tasi kutlamasiysa, kutlanan gun sayisi (7, 30, 100, 365).
    /// Uyari bildirimlerinde null'dir.
    /// <para>
    /// Ayni kilometre tasinin ayni seri icinde iki kez kutlanmasini engellemek icin
    /// kullanilir. Seri kirilip yeniden ayni esige ulasilirsa tekrar kutlanir -
    /// bu yeni bir basaridir.
    /// </para>
    /// </summary>
    public int? MilestoneDay { get; set; }

    /// <summary>Basarisiz gonderimlerde yakalanan hata (orn. GitHub rate-limit mesaji).</summary>
    public string? ErrorMessage { get; set; }

    public DateTime SentAt { get; set; }

    // --- Navigation properties ---

    public User User { get; set; } = null!;
}
