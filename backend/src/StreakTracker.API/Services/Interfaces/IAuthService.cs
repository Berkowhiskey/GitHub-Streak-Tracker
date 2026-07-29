using StreakTracker.API.Models.Auth;

namespace StreakTracker.API.Services.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Kullanicinin yonlendirilecegi GitHub yetkilendirme adresini uretir.
    /// </summary>
    /// <param name="state">CSRF korumasi icin uretilen tek kullanimlik deger.</param>
    string BuildAuthorizationUrl(string state);

    /// <summary>
    /// GitHub'dan donen gecici "code" degerini access token'a cevirir,
    /// kullaniciyi veritabaninda olusturur veya gunceller ve JWT uretir.
    /// </summary>
    Task<AuthResultDto> HandleCallbackAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kimligi dogrulanmis kullanicinin guncel profil ozetini dondurur.
    /// </summary>
    Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
