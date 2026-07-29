namespace StreakTracker.API.Services.Interfaces;

/// <summary>
/// GitHub access token'larini veritabanina yazilmadan once sifreler.
/// Veritabani ele gecse bile token'lar dogrudan kullanilamaz.
/// </summary>
public interface ITokenProtector
{
    string Protect(string plainText);

    /// <summary>
    /// Sifreli metni cozer. Sifreleme oncesi doneme ait duz metin kayitlar icin
    /// degeri oldugu gibi dondurur (kademeli gecis).
    /// </summary>
    string Unprotect(string protectedText);

    /// <summary>
    /// Degerin gercekten sifrelenmis olup olmadigini bildirir.
    /// Sifreleme devreye alinmadan once kaydedilmis duz metin kayitlari tespit etmek icin kullanilir.
    /// </summary>
    bool IsProtected(string value);
}
