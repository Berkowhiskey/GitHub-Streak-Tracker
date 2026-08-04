using System.Xml.Linq;
using StreakTracker.API.Models.Badges;
using StreakTracker.API.Services;

namespace StreakTracker.Tests;

/// <summary>
/// Alev sekilleri rutbeye baglidir: secilebilir degil, KAZANILIR.
/// Secilebilir olsaydi herkes en gorkemlisini secer ve rutbe sistemi anlamini yitirirdi.
/// </summary>
public class FlameLibraryTests
{
    private readonly SvgBadgeService _service = new();

    private static BadgeData Data(int streak) =>
        new("Berkowhiskey", streak, Math.Max(streak, 10), true, new DateOnly(2026, 8, 3));

    [Fact]
    public void Tum_sekiller_yuklenir()
    {
        Assert.NotEmpty(FlameLibrary.All);

        // Her rutbenin bir sekli olmali.
        foreach (var rank in Enum.GetValues<StreakRank>())
            Assert.NotNull(FlameLibrary.For(rank));
    }

    [Theory]
    [InlineData(StreakRank.Spark, "candle-light")]
    [InlineData(StreakRank.Flame, "classic")]
    [InlineData(StreakRank.Fire, "burning-embers")]
    [InlineData(StreakRank.Blaze, "celebration-fire")]
    [InlineData(StreakRank.Legend, "volcano")]
    public void Her_rutbe_kendi_alevini_kullanir(StreakRank rank, string expectedFile)
    {
        Assert.Equal(expectedFile, FlameLibrary.FileNameFor(rank));
        Assert.Equal(expectedFile, FlameLibrary.For(rank).Name);
    }

    /// <summary>
    /// Ayni sekil iki rutbede kullanilirsa gorsel ilerleme hissi kaybolur.
    /// (Kivilcim oncesi "rutbesiz" durum haric - o da mumla cizilir.)
    /// </summary>
    [Fact]
    public void Rutbeler_birbirinden_farkli_sekiller_kullanir()
    {
        var files = new[]
            {
                StreakRank.Spark, StreakRank.Flame, StreakRank.Fire,
                StreakRank.Blaze, StreakRank.Legend,
            }
            .Select(FlameLibrary.FileNameFor)
            .ToList();

        Assert.Equal(files.Count, files.Distinct().Count());
    }

    /// <summary>
    /// Seri buyudukce rozetteki alev gercekten degismeli - bu ozelligin tum amaci bu.
    /// </summary>
    [Theory]
    [InlineData(1, 7)]     // Kivilcim -> Alev
    [InlineData(7, 30)]    // Alev -> Ates
    [InlineData(30, 100)]  // Ates -> Yangin
    [InlineData(100, 365)] // Yangin -> Efsane
    public void Rutbe_atlayinca_rozetteki_alev_degisir(int before, int after)
    {
        var svgBefore = _service.GenerateStreakBadge(Data(before), new BadgeRenderOptions());
        var svgAfter = _service.GenerateStreakBadge(Data(after), new BadgeRenderOptions());

        Assert.NotEqual(svgBefore, svgAfter);

        // Sadece sayi degil, cizilen yol da farkli olmali.
        var pathBefore = FirstPath(svgBefore);
        var pathAfter = FirstPath(svgAfter);

        Assert.NotEqual(pathBefore, pathAfter);
    }

    [Theory]
    [InlineData(BadgeVariant.Full)]
    [InlineData(BadgeVariant.Compact)]
    [InlineData(BadgeVariant.Max)]
    public void Her_varyantta_her_rutbe_gecerli_xml_uretir(BadgeVariant variant)
    {
        foreach (var streak in new[] { 0, 1, 7, 30, 100, 365 })
        {
            var svg = _service.GenerateStreakBadge(
                Data(streak), new BadgeRenderOptions(Variant: variant));

            var exception = Record.Exception(() => XDocument.Parse(svg));

            Assert.True(exception is null,
                $"variant={variant}, streak={streak} gecerli XML degil: {exception?.Message}");
        }
    }

    /// <summary>
    /// Kaynak dosyalar farkli viewBox boyutlarinda geliyor (Game Icons 512, elle
    /// cizilenler 24). Olcek her dosyanin kendi boyutuna gore hesaplanmazsa alev
    /// ya nokta kadar kalir ya da rozetin disina tasar.
    /// </summary>
    [Fact]
    public void Farkli_viewBox_boyutlari_dogru_olceklenir()
    {
        var candle = FlameLibrary.For(StreakRank.Spark);   // 512'lik
        var classic = FlameLibrary.For(StreakRank.Flame);  // 24'luk

        Assert.NotEqual(candle.ViewBoxSize, classic.ViewBoxSize);

        var svgCandle = _service.GenerateStreakBadge(Data(1), new BadgeRenderOptions());
        var svgClassic = _service.GenerateStreakBadge(Data(7), new BadgeRenderOptions());

        // Olcek carpani, hedef boyut (57.6) / viewBox olmali.
        Assert.Contains($"scale({(57.6 / candle.ViewBoxSize):0.##})".Replace(",", "."), svgCandle);
        Assert.Contains($"scale({(57.6 / classic.ViewBoxSize):0.##})".Replace(",", "."), svgClassic);
    }

    [Fact]
    public void Serisi_olmayan_kullanicida_ic_cekirdek_cizilmez()
    {
        var svg = _service.GenerateStreakBadge(Data(0), new BadgeRenderOptions());

        Assert.DoesNotContain("flame-core", svg);
    }

    [Fact]
    public void Bulunamadi_rozeti_de_gecerli_alev_cizer()
    {
        var svg = _service.GenerateNotFoundBadge("kimse", new BadgeRenderOptions());

        XDocument.Parse(svg);
        Assert.Contains("<path", svg);
    }

    private static string FirstPath(string svg) =>
        XDocument.Parse(svg)
            .Descendants()
            .First(e => e.Name.LocalName == "path" && e.Attribute("d") is not null)
            .Attribute("d")!.Value;
}
