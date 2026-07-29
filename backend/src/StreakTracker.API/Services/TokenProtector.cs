using Microsoft.AspNetCore.DataProtection;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.API.Services;

/// <inheritdoc cref="ITokenProtector" />
public class TokenProtector : ITokenProtector
{
    /// <summary>
    /// DataProtection amaci (purpose). Degistirilirse mevcut sifreli degerler cozulemez.
    /// </summary>
    private const string Purpose = "StreakTracker.GitHubAccessToken.v1";

    private readonly IDataProtector _protector;
    private readonly ILogger<TokenProtector> _logger;

    public TokenProtector(IDataProtectionProvider provider, ILogger<TokenProtector> logger)
    {
        _protector = provider.CreateProtector(Purpose);
        _logger = logger;
    }

    public string Protect(string plainText)
    {
        return string.IsNullOrEmpty(plainText) ? plainText : _protector.Protect(plainText);
    }

    public string Unprotect(string protectedText)
    {
        if (string.IsNullOrEmpty(protectedText))
            return protectedText;

        try
        {
            return _protector.Unprotect(protectedText);
        }
        catch (Exception)
        {
            // Sifreleme devreye alinmadan once kaydedilmis duz metin token'lar bu yola duser.
            // Kayit bir sonraki guncellemede otomatik olarak sifreli hale gelir.
            _logger.LogWarning(
                "Access token cozulemedi; sifreleme oncesi kaydedilmis duz metin varsayiliyor. " +
                "Kullanici bir sonraki girisinde deger sifrelenecek.");

            return protectedText;
        }
    }

    public bool IsProtected(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        try
        {
            _protector.Unprotect(value);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
