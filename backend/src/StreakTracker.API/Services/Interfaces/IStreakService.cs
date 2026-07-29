using StreakTracker.API.Entities;
using StreakTracker.API.Models.Badges;

namespace StreakTracker.API.Services.Interfaces;

/// <summary>
/// Kullanicilarin streak verilerini GitHub'dan tazeleyip veritabaninda guncel tutar.
/// </summary>
public interface IStreakService
{
    /// <summary>
    /// Kullanicinin katki takvimini GitHub'dan ceker, streak degerlerini yeniden hesaplar
    /// ve veritabanina yazar. Streak kaydi yoksa olusturur.
    /// </summary>
    /// <returns>Guncellenmis streak kaydi.</returns>
    Task<Streak> UpdateUserStreakAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanicinin mevcut streak kaydini GitHub'a gitmeden veritabanindan okur.
    /// </summary>
    Task<Streak?> GetStreakAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rozet cizimi icin gereken veriyi kullanici adindan tek sorguda okur.
    /// Rozet endpoint'i her istekte cagrildigi icin GitHub'a gidilmez;
    /// yalnizca veritabanindaki guncel deger kullanilir (%100 uptime hedefi).
    /// </summary>
    Task<BadgeData?> GetBadgeDataAsync(string username, CancellationToken cancellationToken = default);
}
