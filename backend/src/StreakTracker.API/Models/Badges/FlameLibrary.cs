using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Xml.Linq;

namespace StreakTracker.API.Models.Badges;

/// <summary>
/// Rozette cizilecek tek bir alev sekli.
/// </summary>
/// <param name="Name">Dosya adi (orn. "candle-light").</param>
/// <param name="ViewBoxSize">Kaynak SVG'nin kare viewBox kenar uzunlugu; olcekleme bundan hesaplanir.</param>
/// <param name="BodyPaths">Alevin govdesini olusturan yollar (gradyanla boyanir).</param>
/// <param name="CorePaths">Varsa ic cekirdek yollari; ayri bir animasyonla titrer.</param>
public sealed record FlameAsset(
    string Name,
    double ViewBoxSize,
    ImmutableArray<string> BodyPaths,
    ImmutableArray<string> CorePaths);

/// <summary>
/// Alev sekilleri kutuphanesi.
/// <para>
/// Sekiller <b>secilebilir degil, kazanilir</b>: kullanicinin rutbesi hangi alevi
/// gorecegini belirler. Secilebilir olsaydi herkes en gorkemlisini secer ve rutbe
/// sistemi anlamini yitirirdi. Kazanilan gorunum, secilen gorunumden daha degerlidir.
/// </para>
/// <para>
/// Dosyalar gomulu kaynak olarak paketlenir; uygulama acilirken bir kez okunup
/// bellekte tutulur - rozet uretiminde disk okumasi yapilmaz.
/// </para>
/// </summary>
public static class FlameLibrary
{
    private const string ResourcePrefix = "StreakTracker.API.Assets.Badges.flames.";

    /// <summary>
    /// Rutbe -> alev dosyasi eslesmesi. Esikler milestone bildirimleriyle ayni
    /// (1 / 7 / 30 / 100 / 365), boylece kutlama bildirimi ile rozetteki gorunum
    /// birbirini dogrular.
    /// </summary>
    private static readonly IReadOnlyDictionary<StreakRank, string> FileByRank =
        new Dictionary<StreakRank, string>
        {
            [StreakRank.None] = "candle-light",
            [StreakRank.Spark] = "candle-light",
            [StreakRank.Flame] = "classic",
            [StreakRank.Fire] = "burning-embers",
            [StreakRank.Blaze] = "celebration-fire",
            [StreakRank.Legend] = "volcano",
        };

    private static readonly Lazy<IReadOnlyDictionary<string, FlameAsset>> Assets =
        new(LoadAll, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Kullanicinin rutbesine karsilik gelen alev seklini dondurur.</summary>
    public static FlameAsset For(StreakRank rank)
    {
        var name = FileByRank.TryGetValue(rank, out var file) ? file : "classic";

        // Bir dosya eksik/bozuksa rozet hic cizilmemektense klasik alevle cizilsin.
        return Assets.Value.TryGetValue(name, out var asset)
            ? asset
            : Assets.Value.Values.First();
    }

    /// <summary>Rutbenin alev dosyasinin adi (arayuzde onizleme icin).</summary>
    public static string FileNameFor(StreakRank rank) =>
        FileByRank.TryGetValue(rank, out var file) ? file : "classic";

    /// <summary>Yuklenen tum sekiller (test ve tanilama icin).</summary>
    public static IReadOnlyDictionary<string, FlameAsset> All => Assets.Value;

    // -----------------------------------------------------------------------
    // Yukleme
    // -----------------------------------------------------------------------

    private static IReadOnlyDictionary<string, FlameAsset> LoadAll()
    {
        var assembly = typeof(FlameLibrary).Assembly;
        var result = new Dictionary<string, FlameAsset>(StringComparer.OrdinalIgnoreCase);

        foreach (var resource in assembly.GetManifestResourceNames())
        {
            if (!resource.StartsWith(ResourcePrefix, StringComparison.Ordinal) ||
                !resource.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = resource[ResourcePrefix.Length..^".svg".Length];
            var asset = Parse(name, assembly, resource);

            if (asset is not null)
                result[name] = asset;
        }

        if (result.Count == 0)
        {
            throw new InvalidOperationException(
                "Hic alev sekli yuklenemedi. Assets/Badges/flames altindaki SVG'ler " +
                "gomulu kaynak olarak paketlenmis mi kontrol edin.");
        }

        return result;
    }

    private static FlameAsset? Parse(string name, Assembly assembly, string resource)
    {
        using var stream = assembly.GetManifestResourceStream(resource);

        if (stream is null)
            return null;

        using var reader = new StreamReader(stream);

        XDocument document;

        try
        {
            document = XDocument.Parse(reader.ReadToEnd());
        }
        catch (System.Xml.XmlException)
        {
            // Bozuk bir dosya tum rozet servisini dusurmemeli; o sekil atlanir.
            return null;
        }

        var viewBoxSize = ReadViewBoxSize(document);

        var body = ImmutableArray.CreateBuilder<string>();
        var core = ImmutableArray.CreateBuilder<string>();

        foreach (var element in document.Descendants().Where(e => e.Name.LocalName == "path"))
        {
            var data = element.Attribute("d")?.Value;

            if (string.IsNullOrWhiteSpace(data))
                continue;

            // class="core" ile isaretli yollar ayri animasyonla titreyen ic bolgedir.
            if (element.Attribute("class")?.Value.Contains("core", StringComparison.OrdinalIgnoreCase) == true)
                core.Add(data);
            else
                body.Add(data);
        }

        return body.Count == 0
            ? null
            : new FlameAsset(name, viewBoxSize, body.ToImmutable(), core.ToImmutable());
    }

    /// <summary>
    /// viewBox'in kenar uzunlugu. Kaynak dosyalar farkli boyutlarda olabiliyor
    /// (Game Icons 512, Lucide 24); olcekleme bu degere gore yapilir.
    /// </summary>
    private static double ReadViewBoxSize(XDocument document)
    {
        var viewBox = document.Root?.Attribute("viewBox")?.Value;

        if (viewBox is not null)
        {
            var parts = viewBox.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 4 &&
                double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var width) &&
                width > 0)
            {
                return width;
            }
        }

        return 24;
    }
}
