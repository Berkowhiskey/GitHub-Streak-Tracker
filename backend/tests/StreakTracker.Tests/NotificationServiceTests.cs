using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StreakTracker.API.Entities;
using StreakTracker.API.Enums;
using StreakTracker.API.Services;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.Tests;

/// <summary>
/// NotificationService, "bildirim gonderilsin mi" kararini veren yerdir ve
/// urunun temel vaadi buna baglidir. Yanlis karar iki yonde de kotudur:
/// gereksiz bildirim rahatsiz eder, eksik bildirim seriyi kaybettirir.
/// </summary>
public class NotificationServiceTests
{
    // -----------------------------------------------------------------------
    // Uyari gonderme karari
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Bugun_commit_atilmissa_uyari_gonderilmez()
    {
        var f = await Fixture.CreateAsync();
        f.SetStreak(currentStreak: 3, hasCommittedToday: true);

        var result = await f.Service.SendStreakWarningAsync(f.User.Id);

        Assert.False(result.Sent);
        Assert.Contains("guvende", result.Reason);
        await f.GitHubApp.DidNotReceive().SendNotificationCommentAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Bugun_commit_yoksa_uyari_gonderilir()
    {
        var f = await Fixture.CreateAsync();
        f.SetStreak(currentStreak: 3, hasCommittedToday: false);

        var result = await f.Service.SendStreakWarningAsync(f.User.Id);

        Assert.True(result.Sent);
        await f.GitHubApp.Received(1).SendNotificationCommentAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Ayni gun icinde ikinci kez uyarilmak rahatsiz edicidir; mukerrer gonderim engellenir.
    /// </summary>
    [Fact]
    public async Task Ayni_gun_ikinci_kez_uyari_gonderilmez()
    {
        var f = await Fixture.CreateAsync();
        f.SetStreak(currentStreak: 3, hasCommittedToday: false);

        var first = await f.Service.SendStreakWarningAsync(f.User.Id);
        var second = await f.Service.SendStreakWarningAsync(f.User.Id);

        Assert.True(first.Sent);
        Assert.False(second.Sent);
        Assert.Contains("zaten", second.Reason);
    }

    /// <summary>
    /// Test bildirimi gondermek, o gunun GERCEK uyarisini engellememelidir.
    /// (NotificationLog.IsTest alani tam olarak bunun icin eklendi.)
    /// </summary>
    [Fact]
    public async Task Test_bildirimi_o_gunun_gercek_uyarisini_engellemez()
    {
        var f = await Fixture.CreateAsync();
        f.SetStreak(currentStreak: 3, hasCommittedToday: false);

        await f.Service.SendTestNotificationAsync(f.User.Id);
        var warning = await f.Service.SendStreakWarningAsync(f.User.Id);

        Assert.True(warning.Sent);
    }

    // -----------------------------------------------------------------------
    // Kilometre taslari
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Kilometre_tasina_ulasildiginda_kutlama_gonderilir()
    {
        var f = await Fixture.CreateAsync();
        f.SetStreak(currentStreak: 7, hasCommittedToday: true);

        var result = await f.Service.SendStreakWarningAsync(f.User.Id);

        // Bugun commit atilmis olmasina ragmen kutlama gonderilir: kutlama uyaridan oncedir.
        Assert.True(result.Sent);

        var log = await f.Db.NotificationLogs.SingleAsync();
        Assert.Equal(7, log.MilestoneDay);
    }

    [Fact]
    public async Task Ayni_kilometre_tasi_ayni_seri_icinde_iki_kez_kutlanmaz()
    {
        var f = await Fixture.CreateAsync();
        f.SetStreak(currentStreak: 7, hasCommittedToday: true);

        var first = await f.Service.SendStreakWarningAsync(f.User.Id);
        var second = await f.Service.SendStreakWarningAsync(f.User.Id);

        Assert.True(first.Sent);
        Assert.False(second.Sent);
    }

    /// <summary>
    /// Seri kirilip yeniden ayni esige ulasilirsa bu YENI bir basaridir:
    /// eski kutlama serinin baslangicindan onceyse tekrar kutlanmalidir.
    /// </summary>
    [Fact]
    public async Task Seri_kirilip_yeniden_ulasilirsa_tekrar_kutlanir()
    {
        var f = await Fixture.CreateAsync();

        // 30 gun onceki bir kutlama: mevcut 7 gunluk serinin baslangicindan cok once.
        f.Db.NotificationLogs.Add(new NotificationLog
        {
            UserId = f.User.Id,
            Channel = NotificationChannel.GitHubIssue,
            Message = "eski kutlama",
            IsSuccess = true,
            IsTest = false,
            MilestoneDay = 7,
            SentAt = DateTime.UtcNow.AddDays(-30),
        });
        await f.Db.SaveChangesAsync();

        f.SetStreak(currentStreak: 7, hasCommittedToday: true);

        var result = await f.Service.SendStreakWarningAsync(f.User.Id);

        Assert.True(result.Sent);
    }

    [Fact]
    public async Task Kilometre_tasi_olmayan_seride_kutlama_gonderilmez()
    {
        var f = await Fixture.CreateAsync();
        f.SetStreak(currentStreak: 6, hasCommittedToday: true);

        var result = await f.Service.SendStreakWarningAsync(f.User.Id);

        Assert.False(result.Sent);
    }

    // -----------------------------------------------------------------------
    // Gonderilemeyen durumlar - sessiz basarisizlik olmamali
    // -----------------------------------------------------------------------

    /// <summary>
    /// App kurulu degilse "gonderildi" denmez. Kullaniciya calismayan bir sistemi
    /// calisiyor gibi gostermek, hic bildirim gondermemekten daha kotudur.
    /// </summary>
    [Fact]
    public async Task GitHub_App_kurulu_degilse_gonderildi_denmez()
    {
        var f = await Fixture.CreateAsync(appInstallationId: null);
        f.GitHubApp.GetInstallationIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((long?)null);
        f.SetStreak(currentStreak: 3, hasCommittedToday: false);

        var result = await f.Service.SendStreakWarningAsync(f.User.Id);

        Assert.False(result.Sent);
        Assert.Contains("GitHub App", result.Reason);
    }

    [Fact]
    public async Task Bildirimler_kapaliysa_gonderilmez()
    {
        var f = await Fixture.CreateAsync(isActive: false);
        f.SetStreak(currentStreak: 3, hasCommittedToday: false);

        var result = await f.Service.SendStreakWarningAsync(f.User.Id);

        Assert.False(result.Sent);
        Assert.Contains("kapali", result.Reason);
    }

    [Fact]
    public async Task Onboarding_tamamlanmamissa_gonderilmez()
    {
        var f = await Fixture.CreateAsync(notificationIssueNumber: null);
        f.SetStreak(currentStreak: 3, hasCommittedToday: false);

        var result = await f.Service.SendStreakWarningAsync(f.User.Id);

        Assert.False(result.Sent);
        Assert.Contains("altyapisi", result.Reason);
    }

    /// <summary>
    /// Basarisiz denemeler de kayda gecmelidir; sorunlar geriye donuk incelenebilsin.
    /// </summary>
    [Fact]
    public async Task Gonderim_hatasi_da_loglanir()
    {
        var f = await Fixture.CreateAsync();
        f.SetStreak(currentStreak: 3, hasCommittedToday: false);

        f.GitHubApp
            .SendNotificationCommentAsync(
                Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("GitHub kapali"));

        var result = await f.Service.SendStreakWarningAsync(f.User.Id);

        Assert.False(result.Sent);

        var log = await f.Db.NotificationLogs.SingleAsync();
        Assert.False(log.IsSuccess);
        Assert.Contains("GitHub kapali", log.ErrorMessage);
    }

    // -----------------------------------------------------------------------
    // Saatlik tur
    // -----------------------------------------------------------------------

    /// <summary>
    /// Bir kullanicida olusan hata (gecersiz token, silinmis repo) turun geri
    /// kalanini durdurmamalidir; aksi halde tek bozuk hesap herkesi etkiler.
    /// </summary>
    [Fact]
    public async Task Bir_kullanicidaki_hata_turu_durdurmaz()
    {
        var f = await Fixture.CreateAsync();

        // Bildirim saati su an olan ikinci bir kullanici ekleyelim.
        var currentHourUtc = DateTime.UtcNow.Hour;
        f.User.PreferredNotificationHour = currentHourUtc;

        var other = TestSupport.CreateUser("digerkullanici", preferredHour: currentHourUtc);
        f.Db.Users.Add(other);
        await f.Db.SaveChangesAsync();

        // Ilk kullanicinin streak tazelemesi patlasin, digerininki calissin.
        f.Streaks.UpdateUserStreakAsync(f.User.Id, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("token gecersiz"));

        f.Streaks.UpdateUserStreakAsync(other.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new Streak
            {
                UserId = other.Id,
                CurrentStreak = 3,
                HasCommittedToday = false,
            }));

        var summary = await f.Service.ProcessHourlyNotificationsAsync(currentHourUtc);

        Assert.Equal(1, summary.Failures);
        Assert.Equal(1, summary.NotificationsSent);
    }

    /// <summary>
    /// Bildirim saati kullanicinin KENDI saat diliminde yorumlanir.
    /// </summary>
    [Fact]
    public async Task Saati_eslesmeyen_kullanici_islenmez()
    {
        var f = await Fixture.CreateAsync();

        // Su anki UTC saatinden farkli bir saat sec.
        f.User.PreferredNotificationHour = (DateTime.UtcNow.Hour + 5) % 24;
        await f.Db.SaveChangesAsync();

        var summary = await f.Service.ProcessHourlyNotificationsAsync(DateTime.UtcNow.Hour);

        Assert.Equal(0, summary.UsersChecked);
        Assert.Equal(0, summary.NotificationsSent);
    }

    // -----------------------------------------------------------------------
    // Test altyapisi
    // -----------------------------------------------------------------------

    private sealed class Fixture
    {
        public required API.Data.AppDbContext Db { get; init; }
        public required User User { get; init; }
        public required IGitHubAppService GitHubApp { get; init; }
        public required IStreakService Streaks { get; init; }
        public required NotificationService Service { get; init; }

        public static async Task<Fixture> CreateAsync(
            bool isActive = true,
            int? notificationIssueNumber = 1,
            long? appInstallationId = 12345)
        {
            var db = TestSupport.CreateDbContext();
            var user = TestSupport.CreateUser(
                isActive: isActive,
                notificationIssueNumber: notificationIssueNumber,
                appInstallationId: appInstallationId);

            db.Users.Add(user);
            await db.SaveChangesAsync();

            var gitHubApp = Substitute.For<IGitHubAppService>();
            gitHubApp.IsConfigured.Returns(true);

            var streaks = Substitute.For<IStreakService>();

            return new Fixture
            {
                Db = db,
                User = user,
                GitHubApp = gitHubApp,
                Streaks = streaks,
                Service = new NotificationService(
                    db, gitHubApp, streaks, NullLogger<NotificationService>.Instance),
            };
        }

        /// <summary>Streak tazelemesinin donecegi sonucu sabitler.</summary>
        public void SetStreak(int currentStreak, bool hasCommittedToday, int longestStreak = 10)
        {
            Streaks.UpdateUserStreakAsync(User.Id, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new Streak
                {
                    UserId = User.Id,
                    CurrentStreak = currentStreak,
                    LongestStreak = longestStreak,
                    HasCommittedToday = hasCommittedToday,
                }));
        }
    }
}
