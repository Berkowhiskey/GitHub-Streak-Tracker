using StreakTracker.API.Entities;

namespace StreakTracker.API.Services.Interfaces;

public interface IJwtTokenService
{
    /// <summary>
    /// Kullanici icin imzali bir JWT uretir.
    /// </summary>
    /// <returns>Token metni ve gecerlilik bitis zamani (UTC).</returns>
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
