using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StreakTracker.API.Data;
using StreakTracker.API.Enums;
using StreakTracker.API.Models.Auth;
using StreakTracker.API.Models.Badges;
using StreakTracker.API.Models.Users;
using StreakTracker.API.Options;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.API.Controllers;

[Route("api/v1/users")]
[Authorize]
public class UsersController : BaseApiController
{
    private readonly AppDbContext _dbContext;
    private readonly IAuthService _authService;
    private readonly IGitHubAppService _gitHubAppService;
    private readonly AppOptions _appOptions;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        AppDbContext dbContext,
        IAuthService authService,
        IGitHubAppService gitHubAppService,
        IOptions<AppOptions> appOptions,
        ILogger<UsersController> logger)
    {
        _dbContext = dbContext;
        _authService = authService;
        _gitHubAppService = gitHubAppService;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// GitHub App kurulum durumunu GitHub'a sorarak dogrular ve veritabanini gunceller.
    /// Kullanici App'i kurduktan sonra "kontrol et" akisinda cagrilir.
    /// </summary>
    [HttpGet("me/app-status")]
    public async Task<ActionResult<AppInstallationStatusDto>> AppStatus(CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId, cancellationToken);

        if (user is null)
            return NotFound();

        if (!_gitHubAppService.IsConfigured)
        {
            return Ok(new AppInstallationStatusDto(
                Installed: false,
                InstallationUrl: _gitHubAppService.InstallationUrl,
                AppConfigured: false));
        }

        var installationId = await _gitHubAppService.GetInstallationIdAsync(
            user.GitHubUsername, cancellationToken);

        // Kurulum kaldirilmis olabilir; veritabanindaki degeri her kontrolde tazeleriz.
        if (user.GitHubAppInstallationId != installationId)
        {
            user.GitHubAppInstallationId = installationId;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "GitHub App kurulum durumu guncellendi. Kullanici: {Username}, Kurulum: {InstallationId}",
                user.GitHubUsername, installationId);
        }

        return Ok(new AppInstallationStatusDto(
            Installed: installationId is not null,
            InstallationUrl: _gitHubAppService.InstallationUrl,
            AppConfigured: true));
    }

    /// <summary>
    /// Giris yapmis kullanicinin profil ozeti.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        var user = await _authService.GetCurrentUserAsync(CurrentUserId, cancellationToken);

        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Bildirim tercihlerini gunceller (bildirim saati, bildirimlerin acik/kapali olmasi).
    /// </summary>
    [HttpPatch("me/preferences")]
    public async Task<ActionResult<CurrentUserDto>> UpdatePreferences(
        [FromBody] UpdatePreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId, cancellationToken);

        if (user is null)
            return NotFound();

        if (request.PreferredNotificationHour is { } hour)
        {
            if (hour is < 0 or > 23)
                throw new ArgumentException("Bildirim saati 0-23 araliginda olmalidir.");

            user.PreferredNotificationHour = hour;
        }

        if (request.TimeZoneId is { } timeZoneId)
        {
            // Taninmayan bir kimlik kaydedilirse tum zaman hesaplari sessizce UTC'ye
            // duser; bu yuzden yazmadan once dogruluyoruz.
            if (!IsValidTimeZone(timeZoneId))
                throw new ArgumentException($"Gecersiz saat dilimi: {timeZoneId}");

            user.TimeZoneId = timeZoneId;
        }

        if (request.Language is { } language)
        {
            if (!AppLanguageExtensions.IsSupported(language))
                throw new ArgumentException($"Desteklenmeyen dil: {language}. Gecerli degerler: tr, en.");

            user.Language = AppLanguageExtensions.ParseLanguage(language);
        }

        if (request.IsActive is { } isActive)
        {
            user.IsActive = isActive;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Bildirim tercihleri guncellendi. Kullanici: {Username}, Saat: {Hour}, Saat dilimi: {TimeZone}, Aktif: {IsActive}",
            user.GitHubUsername, user.PreferredNotificationHour, user.TimeZoneId, user.IsActive);

        var updated = await _authService.GetCurrentUserAsync(CurrentUserId, cancellationToken);

        return Ok(updated);
    }

    /// <summary>
    /// Kullanicinin kayitli rozet gorunum ayarlarini dondurur.
    /// </summary>
    [HttpGet("me/badge-settings")]
    public async Task<ActionResult<BadgeSettingsDto>> GetBadgeSettings(CancellationToken cancellationToken)
    {
        var json = await _dbContext.Users
            .Where(u => u.Id == CurrentUserId)
            .Select(u => u.BadgeSettingsJson)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(ToDto(BadgeSettings.FromJson(json)));
    }

    private static BadgeSettingsDto ToDto(BadgeSettings settings) => new(
        settings.Theme.ToCode(),
        settings.Variant.ToCode(),
        settings.Animated,
        settings.FlameFrom,
        settings.FlameTo,
        settings.Background,
        settings.Border);

    /// <summary>
    /// Rozet gorunum ayarlarini kaydeder ve yeni imzayi uretir.
    /// </summary>
    [HttpPut("me/badge-settings")]
    public async Task<ActionResult<BadgeSettingsDto>> UpdateBadgeSettings(
        [FromBody] UpdateBadgeSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId, cancellationToken);

        if (user is null)
            return NotFound();

        // Renkler burada ayiklanir: gecersiz bir deger veritabanina hic girmez.
        // (Cizim sirasinda ikinci bir dogrulama daha var - savunmanin iki katmani.)
        var settings = new BadgeSettings
        {
            Theme = BadgeRenderOptions.ParseTheme(request.Theme),
            Variant = BadgeRenderOptions.ParseVariant(request.Variant),
            Animated = request.Animated ?? true,
            FlameFrom = request.FlameFrom,
            FlameTo = request.FlameTo,
            Background = request.Background,
            Border = request.Border,
        }.Sanitized();

        user.BadgeSettingsJson = settings.ToJson();
        user.BadgeSettingsSignature = settings.ComputeSignature();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Rozet gorunumu guncellendi. Kullanici: {Username}, Imza: {Signature}",
            user.GitHubUsername, user.BadgeSettingsSignature);

        return Ok(ToDto(settings));
    }

    /// <summary>
    /// Tema adresin parcasi olacaksa dogrulanmis halini dondurur.
    /// Koyu tema varsayilan oldugu icin adrese hic yazilmaz - kisa URL daha temiz.
    /// </summary>
    private static string ThemeQueryValue(string? theme)
    {
        var parsed = BadgeRenderOptions.ParseTheme(theme);

        // Koyu tema varsayilan; adrese yazmiyoruz ki URL kisa kalsin.
        return parsed == BadgeTheme.Dark ? string.Empty : $"&theme={parsed.ToCode()}";
    }

    /// <summary>
    /// Verilen IANA kimliginin sistemde tanimli bir saat dilimine karsilik gelip
    /// gelmedigini kontrol eder.
    /// </summary>
    private static bool IsValidTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return false;

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Profil README'sine yapistirilabilecek hazir rozet kodlarini dondurur.
    /// </summary>
    [HttpGet("me/badge")]
    public async Task<ActionResult<BadgeSnippetsDto>> BadgeSnippets(
        [FromQuery] string? lang,
        [FromQuery] string? theme,
        [FromQuery] string? variant,
        CancellationToken cancellationToken)
    {
        var profile = await _dbContext.Users
            .Where(u => u.Id == CurrentUserId)
            .Select(u => new { u.GitHubUsername, u.Language, u.BadgeSettingsSignature })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
            return NotFound();

        var username = profile.GitHubUsername;

        // Dil URL'e aciktan yaziliyor. Neden: rozetler uzun sureli onbelleklenir
        // (tarayici max-age, ustelik GitHub README'de camo proxy'si). Dil yalnizca
        // kullanicinin kayitli tercihinden okunsaydi ayni URL farkli icerik dondurur
        // ve tercih degistiginde profildeki rozet uzun sure eski dilde kalirdi.
        // Dil oncelikle sorgu parametresinden okunur, yoksa kullanicinin kayitli tercihi.
        // Arayuz hangi dili istedigini zaten biliyor; boylece dil degistirildikten hemen
        // sonra kod parcaciklari istendiginde kaydetme islemi bitmemis olsa bile dogru
        // dil dondurulur (yaris durumu ortadan kalkar).
        var language = lang is not null && AppLanguageExtensions.IsSupported(lang)
            ? AppLanguageExtensions.ParseLanguage(lang)
            : profile.Language;

        var code = language.ToCode();

        // Tema ve varyant da adrese yaziliyor - dil ile ayni gerekce: rozet uzun
        // sureli onbelleklendigi icin gorunumu belirleyen her sey URL'de olmali.
        var themeName = ThemeQueryValue(theme);
        var variantName = BadgeRenderOptions.ParseVariant(variant) == BadgeVariant.Compact
            ? "&variant=compact"
            : string.Empty;

        // Kullanici gorunumunu kaydettiyse adres kisa kalir: ayarlar veritabanindan
        // okunur, adrese yalnizca imza yazilir. Imza ayar degisince degisir ve
        // onbellegin (tarayici + GitHub camo) taze icerik cekmesini saglar.
        var signature = string.IsNullOrEmpty(profile.BadgeSettingsSignature)
            ? string.Empty
            : $"&s={profile.BadgeSettingsSignature}";

        var baseUrl = _appOptions.PublicBaseUrl.TrimEnd('/');
        var badgeUrl = $"{baseUrl}/api/v1/badges/{username}.svg?lang={code}{themeName}{variantName}{signature}";
        var badgeUrlLight = $"{baseUrl}/api/v1/badges/{username}.svg?theme=light&lang={code}{variantName}";
        var profileUrl = $"https://github.com/{username}";

        return Ok(new BadgeSnippetsDto(
            BadgeUrl: badgeUrl,
            BadgeUrlLight: badgeUrlLight,
            Markdown: $"[![GitHub Streak]({badgeUrl})]({profileUrl})",
            Html: $"<a href=\"{profileUrl}\"><img src=\"{badgeUrl}\" alt=\"GitHub Streak\" /></a>"));
    }

    /// <summary>
    /// Kullanicinin StreakTracker kaydini ve tum verilerini siler (KVKK silme hakki).
    /// GitHub hesabindaki gizli repo silinmez; ona yalnizca kullanici karar verebilir.
    /// </summary>
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteAccount(CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId, cancellationToken);

        if (user is null)
            return NotFound();

        var username = user.GitHubUsername;
        var repoName = user.PrivateNotificationRepoName;

        // Streak ve bildirim kayitlari cascade delete ile birlikte silinir.
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Kullanici kaydi silindi: {Username}", username);

        return Ok(new
        {
            deleted = true,
            message = repoName is null
                ? "Hesabiniz ve tum verileriniz silindi."
                : $"Hesabiniz ve tum verileriniz silindi. GitHub hesabinizdaki '{repoName}' reposu duruyor; " +
                  "dilerseniz GitHub uzerinden kendiniz silebilirsiniz."
        });
    }
}
