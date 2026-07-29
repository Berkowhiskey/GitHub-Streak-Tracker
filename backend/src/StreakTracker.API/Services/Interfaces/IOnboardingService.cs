using StreakTracker.API.Models.Auth;

namespace StreakTracker.API.Services.Interfaces;

public interface IOnboardingService
{
    /// <summary>
    /// Kullanicinin onayini kaydeder, gizli bildirim reposu ile sabit Issue'yu kurar
    /// ve ilk streak hesaplamasini yapar. Islem idempotenttir: tekrar cagrilirsa
    /// var olan kurulum bozulmaz.
    /// </summary>
    /// <exception cref="InvalidOperationException">Kullanici onay vermemisse firlatilir.</exception>
    Task<OnboardingResultDto> CompleteOnboardingAsync(
        Guid userId,
        OnboardingRequest request,
        CancellationToken cancellationToken = default);
}
