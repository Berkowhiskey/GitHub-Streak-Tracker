using StreakTracker.API.Models.GitHub;

namespace StreakTracker.API.Services;

/// <param name="CurrentStreak">Devam eden kesintisiz gun sayisi.</param>
/// <param name="LongestStreak">Verilen pencere icindeki en uzun kesintisiz seri.</param>
/// <param name="LastCommitDate">Katki tespit edilen son gun.</param>
/// <param name="HasCommittedToday">Bugun katki yapilip yapilmadigi.</param>
public record StreakCalculationResult(
    int CurrentStreak,
    int LongestStreak,
    DateOnly? LastCommitDate,
    bool HasCommittedToday);

/// <summary>
/// Katki takviminden streak degerlerini hesaplayan saf (yan etkisiz) mantik.
/// Veritabani veya GitHub bagimliligi yoktur; bu sayede dogrudan test edilebilir.
/// </summary>
public static class StreakCalculator
{
    /// <summary>
    /// Gunluk katki listesinden streak durumunu hesaplar.
    /// </summary>
    /// <param name="days">Katki gunleri (sirali olmasi gerekmez, tekrar edenler tekillestirilir).</param>
    /// <param name="today">Hesaplamanin referans aldigi bugunun UTC tarihi.</param>
    public static StreakCalculationResult Calculate(IReadOnlyList<ContributionDay> days, DateOnly today)
    {
        var activeDays = days
            .Where(d => d.HasContribution && d.Date <= today)
            .Select(d => d.Date)
            .ToHashSet();

        if (activeDays.Count == 0)
        {
            return new StreakCalculationResult(0, 0, null, false);
        }

        var hasCommittedToday = activeDays.Contains(today);
        var lastCommitDate = activeDays.Max();

        return new StreakCalculationResult(
            CurrentStreak: CalculateCurrentStreak(activeDays, today, hasCommittedToday),
            LongestStreak: CalculateLongestStreak(activeDays),
            LastCommitDate: lastCommitDate,
            HasCommittedToday: hasCommittedToday);
    }

    /// <summary>
    /// Devam eden seriyi hesaplar.
    /// Kritik kural: bugun henuz commit yoksa seri KIRILMIS sayilmaz; gun daha bitmemistir.
    /// Bu durumda seri dunden geriye dogru sayilir ve kullaniciya "serin tehlikede" bildirimi
    /// gonderilmesini anlamli kilan deger budur. Seri ancak dun de commit yoksa sifirlanir.
    /// </summary>
    private static int CalculateCurrentStreak(HashSet<DateOnly> activeDays, DateOnly today, bool hasCommittedToday)
    {
        var cursor = hasCommittedToday ? today : today.AddDays(-1);

        // Ne bugun ne de dun katki varsa seri tamamen kirilmistir.
        if (!activeDays.Contains(cursor))
        {
            return 0;
        }

        var streak = 0;

        while (activeDays.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    /// <summary>
    /// Verilen pencere icindeki en uzun kesintisiz seriyi bulur.
    /// Not: Pencere disinda kalan daha eski bir rekor bu hesaba dahil olmaz;
    /// bu yuzden cagiran katman veritabanindaki mevcut rekorla karsilastirmalidir.
    /// </summary>
    private static int CalculateLongestStreak(HashSet<DateOnly> activeDays)
    {
        var longest = 0;

        foreach (var day in activeDays)
        {
            // Yalnizca serilerin baslangic gunlerinden ileri dogru say; boylece her seri bir kez islenir.
            if (activeDays.Contains(day.AddDays(-1)))
                continue;

            var length = 0;
            var cursor = day;

            while (activeDays.Contains(cursor))
            {
                length++;
                cursor = cursor.AddDays(1);
            }

            if (length > longest)
                longest = length;
        }

        return longest;
    }
}
