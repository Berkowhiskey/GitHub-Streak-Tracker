using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StreakTracker.API.Models.GitHub;
using StreakTracker.API.Services;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.Tests;

/// <summary>
/// StreakService, GitHub'dan cekilen katki takvimini streak kaydina donusturur.
/// Buradaki en kritik test, katki penceresinin genisligini sabitleyen testtir:
/// 31 Temmuz 2026'da uretimde yasanan hata tam olarak orada cikmisti.
/// </summary>
public class StreakServiceTests
{
    /// <summary>
    /// GitHub'in contributionsCollection sorgusu en fazla 1 YILLIK aralik kabul eder.
    /// Asildiginda hata donmez; son gunun katki sayisi sessizce 0 gelir. Bu yuzden
    /// "bugun commit attim ama sistem gormuyor" hatasi olusmustu.
    ///
    /// Ust sinir 365 DEGIL 364'tur. Uretimde ayni gun, ayni kullaniciyla olculdu:
    ///   365 gunluk pencere -> {"date":"2026-07-31","contributionCount":0}  (hatali)
    ///   364 gunluk pencere -> {"date":"2026-07-31","contributionCount":1}  (dogru)
    /// Yani 365'in kendisi zaten bozuk davraniyor; sinir onun bir altinda.
    /// </summary>
    [Fact]
    public async Task UpdateUserStreakAsync_katki_penceresi_GitHubin_bir_yillik_sinirini_asmaz()
    {
        await using var db = TestSupport.CreateDbContext();
        var user = TestSupport.CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var gitHub = Substitute.For<IGitHubService>();
        DateOnly capturedFrom = default;
        DateOnly capturedTo = default;

        gitHub
            .GetContributionDaysAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedFrom = call.ArgAt<DateOnly>(2);
                capturedTo = call.ArgAt<DateOnly>(3);
                return Task.FromResult<IReadOnlyList<ContributionDay>>([]);
            });

        var service = new StreakService(db, gitHub, NullLogger<StreakService>.Instance);

        await service.UpdateUserStreakAsync(user.Id);

        // Iki uc da dahil oldugu icin gun sayisi = fark + 1.
        var totalDays = capturedTo.DayNumber - capturedFrom.DayNumber + 1;

        Assert.True(
            totalDays <= 364,
            $"Katki penceresi {totalDays} gun. 365 ve uzeri pencerelerde GitHub son gunun " +
            "katkisini sessizce 0 donduruyor; kullanici commit atsa bile sistem " +
            "'bugun commit yok' diyor. Pencere en fazla 364 gun olmali.");
    }

    /// <summary>
    /// Pencere gereksiz yere daraltilmamali: streak gecmisi icin bir yila yakin
    /// veri cekildigini de sabitliyoruz (yanlislikla 30 gune dusurulurse yakalanir).
    /// </summary>
    [Fact]
    public async Task UpdateUserStreakAsync_yaklasik_bir_yillik_gecmis_ceker()
    {
        await using var db = TestSupport.CreateDbContext();
        var user = TestSupport.CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var gitHub = Substitute.For<IGitHubService>();
        DateOnly capturedFrom = default;
        DateOnly capturedTo = default;

        gitHub
            .GetContributionDaysAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedFrom = call.ArgAt<DateOnly>(2);
                capturedTo = call.ArgAt<DateOnly>(3);
                return Task.FromResult<IReadOnlyList<ContributionDay>>([]);
            });

        var service = new StreakService(db, gitHub, NullLogger<StreakService>.Instance);

        await service.UpdateUserStreakAsync(user.Id);

        var totalDays = capturedTo.DayNumber - capturedFrom.DayNumber + 1;

        Assert.True(totalDays >= 360, $"Katki penceresi beklenenden dar: {totalDays} gun.");
    }

    /// <summary>
    /// Sorgunun bitisi kullanicinin BUGUNU olmali; aksi halde bugun atilan commit
    /// hicbir zaman goruntulenmez.
    /// </summary>
    [Fact]
    public async Task UpdateUserStreakAsync_pencerenin_bitisi_kullanicinin_bugunudur()
    {
        await using var db = TestSupport.CreateDbContext();
        var user = TestSupport.CreateUser(timeZoneId: "Europe/Istanbul");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var gitHub = Substitute.For<IGitHubService>();
        DateOnly capturedTo = default;

        gitHub
            .GetContributionDaysAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedTo = call.ArgAt<DateOnly>(3);
                return Task.FromResult<IReadOnlyList<ContributionDay>>([]);
            });

        var service = new StreakService(db, gitHub, NullLogger<StreakService>.Instance);

        await service.UpdateUserStreakAsync(user.Id);

        var expectedToday = UserClock.TodayIn(UserClock.Resolve("Europe/Istanbul"));

        Assert.Equal(expectedToday, capturedTo);
    }

    [Fact]
    public async Task UpdateUserStreakAsync_bugun_katki_varsa_HasCommittedToday_true_olur()
    {
        await using var db = TestSupport.CreateDbContext();
        var user = TestSupport.CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var gitHub = StubGitHub([
            new ContributionDay(today.AddDays(-1), 3),
            new ContributionDay(today, 1),
        ]);

        var service = new StreakService(db, gitHub, NullLogger<StreakService>.Instance);

        var streak = await service.UpdateUserStreakAsync(user.Id);

        Assert.True(streak.HasCommittedToday);
        Assert.Equal(2, streak.CurrentStreak);
        Assert.Equal(today, streak.LastCommitDate);
    }

    /// <summary>
    /// Cekilen 1 yillik pencere daha eski bir rekoru goremeyebilir; bu yuzden
    /// kayitli rekor asla dusurulmez.
    /// </summary>
    [Fact]
    public async Task UpdateUserStreakAsync_kayitli_rekoru_asla_dusurmez()
    {
        await using var db = TestSupport.CreateDbContext();
        var user = TestSupport.CreateUser();
        db.Users.Add(user);
        db.Streaks.Add(new API.Entities.Streak { UserId = user.Id, LongestStreak = 99 });
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var gitHub = StubGitHub([new ContributionDay(today, 1)]);

        var service = new StreakService(db, gitHub, NullLogger<StreakService>.Instance);

        var streak = await service.UpdateUserStreakAsync(user.Id);

        Assert.Equal(99, streak.LongestStreak);
    }

    [Fact]
    public async Task UpdateUserStreakAsync_streak_kaydi_yoksa_olusturur()
    {
        await using var db = TestSupport.CreateDbContext();
        var user = TestSupport.CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var gitHub = StubGitHub([]);
        var service = new StreakService(db, gitHub, NullLogger<StreakService>.Instance);

        await service.UpdateUserStreakAsync(user.Id);

        Assert.True(await db.Streaks.AnyAsync(s => s.UserId == user.Id));
    }

    [Fact]
    public async Task UpdateUserStreakAsync_kullanici_yoksa_hata_firlatir()
    {
        await using var db = TestSupport.CreateDbContext();
        var service = new StreakService(db, StubGitHub([]), NullLogger<StreakService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateUserStreakAsync(Guid.NewGuid()));
    }

    private static IGitHubService StubGitHub(IReadOnlyList<ContributionDay> days)
    {
        var gitHub = Substitute.For<IGitHubService>();

        gitHub
            .GetContributionDaysAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(days));

        return gitHub;
    }
}
