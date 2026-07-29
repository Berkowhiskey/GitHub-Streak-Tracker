namespace StreakTracker.API.Enums;

/// <summary>
/// Bildirimin kullaniciya ulastirildigi kanal.
/// Birincil kanal GitHubIssue'dur (GitHub Mobile push bildirimi tetikler);
/// digerleri Faz 3'te devreye alinacak fallback kanallaridir.
/// </summary>
public enum NotificationChannel
{
    /// <summary>Gizli repodaki sabit Issue'ya @mention'li yorum atilir -> GitHub Mobile push bildirimi.</summary>
    GitHubIssue = 0,

    Telegram = 1,

    Email = 2
}
