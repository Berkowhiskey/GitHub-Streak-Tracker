using System.Reflection;
using System.Xml.Linq;
using StreakTracker.API.Services;

namespace StreakTracker.Tests;

/// <summary>
/// Rozet gorselleri (alev sekilleri ve ekipmanlar) gomulu kaynak olarak paketlenir.
/// Bu testler, dosyalarin gercekten pakete girdigini ve kullanilabilir durumda
/// oldugunu dogrular - aksi halde eksiklik ancak calisma aninda fark edilirdi.
/// </summary>
public class BadgeAssetsTests
{
    private static readonly Assembly ApiAssembly = typeof(SvgBadgeService).Assembly;

    private static string[] AssetNames() => ApiAssembly
        .GetManifestResourceNames()
        .Where(n => n.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        .ToArray();

    [Fact]
    public void Gorseller_pakete_gomulu_gelir()
    {
        var names = AssetNames();

        Assert.NotEmpty(names);
        Assert.Contains(names, n => n.Contains("flames") && n.EndsWith("classic.svg"));
    }

    /// <summary>
    /// Bozuk bir SVG rozeti komple kirar; her dosya ayristirilabilir olmali.
    /// </summary>
    [Fact]
    public void Tum_gorseller_gecerli_xml()
    {
        foreach (var name in AssetNames())
        {
            using var stream = ApiAssembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);

            var content = reader.ReadToEnd();

            var exception = Record.Exception(() => XDocument.Parse(content));

            Assert.True(exception is null, $"{name} gecerli XML degil: {exception?.Message}");
        }
    }

    /// <summary>
    /// Sekillerde sabit renk olmamali: rengi rozet gradyanla veriyor.
    /// Dosyada fill/stroke varsa kullanicinin renk secimi calismaz.
    /// </summary>
    [Fact]
    public void Gorsellerde_sabit_renk_bulunmaz()
    {
        foreach (var name in AssetNames())
        {
            using var stream = ApiAssembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);

            var content = reader.ReadToEnd();

            Assert.False(
                content.Contains("fill=\"#", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("stroke=\"#", StringComparison.OrdinalIgnoreCase),
                $"{name} sabit renk iceriyor. Rengi rozet veriyor; dosyadan fill/stroke kaldirilmali.");
        }
    }

    /// <summary>
    /// Her sekil en az bir cizim yolu icermeli; bos dosya rozeti sessizce bozar.
    /// </summary>
    [Fact]
    public void Her_gorsel_en_az_bir_path_icerir()
    {
        foreach (var name in AssetNames())
        {
            using var stream = ApiAssembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);

            var document = XDocument.Parse(reader.ReadToEnd());

            var paths = document.Descendants()
                .Count(e => e.Name.LocalName == "path");

            Assert.True(paths > 0, $"{name} icinde hic <path> yok.");
        }
    }
}
