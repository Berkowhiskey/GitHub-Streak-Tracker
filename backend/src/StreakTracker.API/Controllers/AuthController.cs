using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StreakTracker.API.Models.Auth;
using StreakTracker.API.Options;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.API.Controllers;

[Route("api/v1/auth")]
public class AuthController : BaseApiController
{
    /// <summary>CSRF korumasi icin uretilen state degerinin saklandigi cerez.</summary>
    private const string StateCookieName = "streaktracker_oauth_state";

    /// <summary>
    /// JWT'nin saklandigi cerez. HttpOnly oldugu icin tarayicidaki JavaScript
    /// tarafindan okunamaz; XSS ile token calinmasina karsi localStorage'dan daha guvenlidir.
    /// </summary>
    public const string AuthCookieName = "streaktracker_token";

    private readonly IAuthService _authService;
    private readonly AppOptions _appOptions;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IOptions<AppOptions> appOptions,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Kullaniciyi GitHub yetkilendirme ekranina yonlendirir.
    /// Tarayicidan dogrudan acilmalidir.
    /// </summary>
    [HttpGet("github/login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        // Tek kullanimlik state: callback'e donen istegin gercekten bizim
        // baslattigimiz akisa ait oldugunu dogrular (CSRF korumasi).
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        Response.Cookies.Append(StateCookieName, state, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax, // GitHub'dan geri donuste cerezin gonderilebilmesi icin
            Expires = DateTimeOffset.UtcNow.AddMinutes(10),
            Path = "/"
        });

        return Redirect(_authService.BuildAuthorizationUrl(state));
    }

    /// <summary>
    /// GitHub yetkilendirme sonrasi geri donus adresi.
    /// Gecici "code" degerini access token'a cevirir ve JWT uretir.
    /// </summary>
    [HttpGet("github/callback")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResultDto>> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription,
        CancellationToken cancellationToken)
    {
        // Kullanici GitHub ekraninda izni reddetmis olabilir.
        if (!string.IsNullOrWhiteSpace(error))
        {
            _logger.LogWarning("GitHub yetkilendirmesi reddedildi: {Error} - {Description}", error, errorDescription);
            return RedirectToFrontend("/?error=access_denied");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return RedirectToFrontend("/?error=missing_code");
        }

        var expectedState = Request.Cookies[StateCookieName];
        Response.Cookies.Delete(StateCookieName);

        if (string.IsNullOrWhiteSpace(expectedState) ||
            string.IsNullOrWhiteSpace(state) ||
            !CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(expectedState),
                System.Text.Encoding.UTF8.GetBytes(state)))
        {
            _logger.LogWarning("OAuth state dogrulamasi basarisiz.");
            return RedirectToFrontend("/?error=invalid_state");
        }

        var result = await _authService.HandleCallbackAsync(code, cancellationToken);

        SetAuthCookie(result.Token, result.ExpiresAt);

        // Onay verilmemis kullanici once bilgilendirme/onay ekranina gider.
        return RedirectToFrontend(result.RequiresOnboarding ? "/onboarding" : "/dashboard");
    }

    /// <summary>
    /// Oturumu kapatir: kimlik dogrulama cerezini siler.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthCookieName, BuildAuthCookieOptions());

        return Ok(new { loggedOut = true });
    }

    private void SetAuthCookie(string token, DateTime expiresAt)
    {
        Response.Cookies.Append(AuthCookieName, token, BuildAuthCookieOptions(options =>
            options.Expires = new DateTimeOffset(expiresAt, TimeSpan.Zero)));
    }

    /// <summary>
    /// Kimlik dogrulama cerezinin ayarlarini uretir.
    /// <para>
    /// SameSite politikasi yapilandirmadan gelir: frontend ile API ayni site altindaysa
    /// <c>Lax</c>, farkli alan adlarindaysa <c>None</c> gerekir. <c>None</c> secildiginde
    /// tarayicilar cerezi yalnizca Secure ise kabul eder.
    /// </para>
    /// Cerezin silinmesi ile olusturulmasi <b>ayni</b> ayarlari kullanmalidir;
    /// aksi halde tarayici silme istegini eslestiremez ve oturum kapanmaz.
    /// </summary>
    private CookieOptions BuildAuthCookieOptions(Action<CookieOptions>? customize = null)
    {
        var sameSite = _appOptions.ResolveCookieSameSite();

        var options = new CookieOptions
        {
            HttpOnly = true,
            // SameSite=None, Secure olmadan gecersizdir.
            Secure = Request.IsHttps || sameSite == SameSiteMode.None,
            SameSite = sameSite,
            Path = "/"
        };

        customize?.Invoke(options);

        return options;
    }

    private RedirectResult RedirectToFrontend(string path) =>
        Redirect($"{_appOptions.FrontendBaseUrl.TrimEnd('/')}{path}");

    /// <summary>
    /// Gecerli JWT ile giris yapmis kullanicinin profil ozetini dondurur.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        var user = await _authService.GetCurrentUserAsync(CurrentUserId, cancellationToken);

        return user is null ? NotFound() : Ok(user);
    }
}
