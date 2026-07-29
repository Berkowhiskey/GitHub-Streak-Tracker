namespace StreakTracker.API.Entities;

/// <summary>
/// Bir kullanicinin kesintisiz commit serisi (streak) durumu.
/// SVG rozet servisi dogrudan bu tablodan okur; bu yuzden her zaman guncel tutulmalidir.
/// </summary>
public class Streak
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Devam etmekte olan kesintisiz gun sayisi.</summary>
    public int CurrentStreak { get; set; }

    /// <summary>Kullanicinin bugune kadar ulastigi en uzun seri.</summary>
    public int LongestStreak { get; set; }

    /// <summary>Commit tespit edilen son gun (UTC tarihi). Hic commit yoksa null.</summary>
    public DateOnly? LastCommitDate { get; set; }

    /// <summary>Icinde bulunulan UTC gununde commit atilip atilmadigi. Bildirim motorunun ana kararidir.</summary>
    public bool HasCommittedToday { get; set; }

    /// <summary>Bu kaydin GitHub API'sine karsi en son ne zaman dogrulandigi.</summary>
    public DateTime LastCheckedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // --- Navigation properties ---

    public User User { get; set; } = null!;
}
