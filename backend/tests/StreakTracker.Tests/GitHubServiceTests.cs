using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using StreakTracker.API.Exceptions;
using StreakTracker.API.Options;
using StreakTracker.API.Services;

namespace StreakTracker.Tests;

/// <summary>
/// GitHubService'in GraphQL katmani. 31 Temmuz 2026'daki "bugun commit gorunmuyor"
/// hatasi tam olarak burada uretilen sorgunun tarih araliginda cikmisti; o zamana
/// kadar bu katmanin hic testi yoktu.
/// </summary>
public class GitHubServiceTests
{
    /// <summary>
    /// Bitis zamani asla gelecege tasmamali. Gun sonuna (23:59:59Z) sorgu atmak,
    /// gunun buyuk bolumunde GitHub'a henuz yasanmamis bir ana kadar sormak demektir.
    /// </summary>
    [Fact]
    public async Task Sorgunun_bitis_zamani_gelecege_tasmaz()
    {
        var (service, captured) = CreateService(EmptyCalendarJson());

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await service.GetContributionDaysAsync("token", "user", today.AddDays(-10), today);

        var to = ParseUtc(captured.Variables!["to"]!);

        Assert.True(
            to <= DateTime.UtcNow.AddSeconds(5),
            $"Sorgunun bitisi gelecekte: {to:O}. Bitis her zaman su ana kirpilmalidir.");
    }

    /// <summary>
    /// Gecmis bir gun sorgulandiginda bitis o gunun sonu olmali; su ana kirpma
    /// yalnizca BUGUN icin gecerlidir, gecmis gunleri kirpmamalidir.
    /// </summary>
    [Fact]
    public async Task Gecmis_gun_sorgusunda_gunun_tamami_kapsanir()
    {
        var (service, captured) = CreateService(EmptyCalendarJson());

        var dun = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        await service.GetContributionDaysAsync("token", "user", dun.AddDays(-5), dun);

        var to = ParseUtc(captured.Variables!["to"]!);

        Assert.Equal(dun, DateOnly.FromDateTime(to));
        Assert.Equal(23, to.Hour);
        Assert.Equal(59, to.Minute);
    }

    [Fact]
    public async Task Baslangic_gunun_ilk_anidir()
    {
        var (service, captured) = CreateService(EmptyCalendarJson());

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-30);
        await service.GetContributionDaysAsync("token", "user", from, today);

        var parsedFrom = ParseUtc(captured.Variables!["from"]!);

        Assert.Equal(from, DateOnly.FromDateTime(parsedFrom));
        Assert.Equal(TimeSpan.Zero, parsedFrom.TimeOfDay);
    }

    [Fact]
    public async Task Baslangic_bitisten_sonraysa_hata_firlatir()
    {
        var (service, _) = CreateService(EmptyCalendarJson());

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetContributionDaysAsync("token", "user", today, today.AddDays(-1)));
    }

    [Fact]
    public async Task Yanittaki_gunler_dogru_cozumlenir()
    {
        var json = """
        {"data":{"user":{"contributionsCollection":{"contributionCalendar":{"weeks":[
          {"contributionDays":[
            {"date":"2026-07-30","contributionCount":5},
            {"date":"2026-07-31","contributionCount":1}
          ]}
        ]}}}}}
        """;

        var (service, _) = CreateService(json);

        var days = await service.GetContributionDaysAsync(
            "token", "user", new DateOnly(2026, 7, 30), new DateOnly(2026, 7, 31));

        Assert.Equal(2, days.Count);
        Assert.Equal(new DateOnly(2026, 7, 31), days[1].Date);
        Assert.Equal(1, days[1].ContributionCount);
        Assert.True(days[1].HasContribution);
    }

    /// <summary>
    /// Rate-limit ayirt edilebilmeli: arka plan job'lari yeniden deneme kararini
    /// bu bilgiye gore verir.
    /// </summary>
    [Fact]
    public async Task Rate_limit_yaniti_ayirt_edilir()
    {
        var (service, _) = CreateService(
            """{"errors":[{"type":"RATE_LIMITED","message":"API rate limit exceeded"}]}""");

        var ex = await Assert.ThrowsAsync<GitHubServiceException>(
            () => service.GetContributionDaysAsync(
                "token", "user", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
                DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.True(ex.IsRateLimited);
    }

    [Fact]
    public async Task HTTP_hatasi_GitHubServiceException_olarak_yuzeye_cikar()
    {
        var (service, _) = CreateService("bozuk", HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<GitHubServiceException>(
            () => service.GetContributionDaysAsync(
                "token", "user", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
                DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    // -----------------------------------------------------------------------
    // Test altyapisi
    // -----------------------------------------------------------------------

    /// <summary>
    /// Sorgudaki tarihler UTC'dir. Duz <c>DateTime.Parse</c> "Z" ekini gorup degeri
    /// makinenin yerel saatine cevirir; testler o zaman calisan makinenin saat
    /// dilimine gore sonuc verir (TR'de +3 saat kayar). Bu yuzden acikca UTC istiyoruz.
    /// </summary>
    private static DateTime ParseUtc(string value) =>
        DateTime.Parse(
            value,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal |
            System.Globalization.DateTimeStyles.AssumeUniversal);

    private static string EmptyCalendarJson() =>
        """{"data":{"user":{"contributionsCollection":{"contributionCalendar":{"weeks":[]}}}}}""";

    private static (GitHubService Service, CapturedRequest Captured) CreateService(
        string responseJson,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var captured = new CapturedRequest();
        var handler = new CapturingHandler(captured, responseJson, statusCode);

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(handler));

        var options = Microsoft.Extensions.Options.Options.Create(new GitHubOptions
        {
            ClientId = "test",
            ClientSecret = "test",
            ProductHeaderName = "StreakTracker-Tests",
        });

        return (new GitHubService(factory, options, NullLogger<GitHubService>.Instance), captured);
    }

    private sealed class CapturedRequest
    {
        public Dictionary<string, string?>? Variables { get; set; }
    }

    /// <summary>
    /// Giden GraphQL istegini yakalar ve sabit bir yanit dondurur; boylece
    /// gercek bir HTTP cagrisi yapmadan sorgunun icerigi dogrulanabilir.
    /// </summary>
    private sealed class CapturingHandler(
        CapturedRequest captured,
        string responseJson,
        HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var payload = await request.Content!.ReadAsStringAsync(cancellationToken);

            using var document = JsonDocument.Parse(payload);
            var variables = document.RootElement.GetProperty("variables");

            captured.Variables = new Dictionary<string, string?>
            {
                ["from"] = variables.GetProperty("from").GetString(),
                ["to"] = variables.GetProperty("to").GetString(),
                ["login"] = variables.GetProperty("login").GetString(),
            };

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            };
        }
    }
}
