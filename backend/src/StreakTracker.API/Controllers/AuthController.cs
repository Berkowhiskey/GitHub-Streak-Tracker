using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreakTracker.API.Models.Auth;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.API.Controllers;

[Route("api/v1/auth")]
public class AuthController : BaseApiController
{
    /// <summary>CSRF korumasi icin uretilen state degerinin saklandigi cerez.</summary>
    private const string StateCookieName = "streaktracker_oauth_state";

    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
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
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "GitHub yetkilendirmesi tamamlanmadi",
                Detail = errorDescription ?? error
            });
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Eksik parametre",
                Detail = "GitHub'dan 'code' parametresi alinamadi."
            });
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
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Gecersiz oturum",
                Detail = "Yetkilendirme dogrulamasi basarisiz oldu. Lutfen girisi bastan baslatin."
            });
        }

        var result = await _authService.HandleCallbackAsync(code, cancellationToken);

        return Ok(result);
    }

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
