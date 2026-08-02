using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StreakTracker.API.Models.Badges;

/// <summary>
/// Kullanicinin kaydettigi rozet gorunum tercihleri.
/// <para>
/// Veritabaninda JSON olarak tutulur; rozet adresine yalnizca kisa bir imza
/// (<c>?s=</c>) yazilir. Boylece adres okunabilir kalir ama ayar degisimi
/// onbellege takilmaz.
/// </para>
/// </summary>
public record BadgeSettings
{
    public BadgeTheme Theme { get; init; } = BadgeTheme.Dark;

    public BadgeVariant Variant { get; init; } = BadgeVariant.Full;

    public bool Animated { get; init; } = true;

    /// <summary>Alev gradyaninin ust rengi (#rrggbb). Null ise temanin rengi kullanilir.</summary>
    public string? FlameFrom { get; init; }

    /// <summary>Alev gradyaninin alt rengi (#rrggbb). Null ise temanin rengi kullanilir.</summary>
    public string? FlameTo { get; init; }

    /// <summary>Rozet arka plani (#rrggbb). Null ise temanin rengi kullanilir.</summary>
    public string? Background { get; init; }

    /// <summary>Kenarlik rengi (#rrggbb). Null ise temanin rengi kullanilir.</summary>
    public string? Border { get; init; }

    public static BadgeSettings Default { get; } = new();

    // -----------------------------------------------------------------------
    // Dogrulama
    // -----------------------------------------------------------------------

    /// <summary>
    /// Yalnizca #rgb ve #rrggbb kabul edilir.
    /// <para>
    /// <b>Guvenlik acisindan kritik:</b> bu deger dogrudan SVG metnine yaziliyor.
    /// Serbest metne izin verilseydi kullanici tirnak kapatip kendi ozniteliklerini
    /// veya elemanlarini enjekte edebilirdi. Bu yuzden beyaz liste kullaniliyor:
    /// desene uymayan deger kabul edilmez (kacislamak yerine reddetmek daha guvenli).
    /// </para>
    /// </summary>
    private static readonly Regex HexColorPattern =
        new("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", RegexOptions.Compiled);

    public static bool IsValidColor(string? value) =>
        value is not null && HexColorPattern.IsMatch(value);

    /// <summary>
    /// Gecersiz renkleri ayiklanmis bir kopya dondurur.
    /// Tek bir hatali renk yuzunden tum ayarlari reddetmek yerine, o alan
    /// temanin varsayilanina birakilir.
    /// </summary>
    public BadgeSettings Sanitized() => this with
    {
        FlameFrom = IsValidColor(FlameFrom) ? FlameFrom : null,
        FlameTo = IsValidColor(FlameTo) ? FlameTo : null,
        Background = IsValidColor(Background) ? Background : null,
        Border = IsValidColor(Border) ? Border : null,
    };

    // -----------------------------------------------------------------------
    // Saklama ve imza
    // -----------------------------------------------------------------------

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>
    /// Kayitli JSON'u okur. Bozuk veya eski bir kayit uygulamayi dusurmemeli;
    /// cozulemezse varsayilan gorunume dusulur.
    /// </summary>
    public static BadgeSettings FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Default;

        try
        {
            return JsonSerializer.Deserialize<BadgeSettings>(json, JsonOptions)?.Sanitized() ?? Default;
        }
        catch (JsonException)
        {
            return Default;
        }
    }

    /// <summary>
    /// Ayarlarin kisa imzasi. Ayni ayar her zaman ayni imzayi uretir;
    /// herhangi bir alan degisince imza da degisir.
    /// </summary>
    public string ComputeSignature()
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(ToJson()));

        return Convert.ToHexString(hash)[..8].ToLowerInvariant();
    }

    /// <summary>Kayitli ayarlari cizim seceneklerine cevirir.</summary>
    public BadgeRenderOptions ToRenderOptions(Enums.AppLanguage language) =>
        new(Theme, language, Variant, Animated)
        {
            FlameFrom = FlameFrom,
            FlameTo = FlameTo,
            Background = Background,
            Border = Border,
        };
}
