using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Octokit;
using StreakTracker.API.Exceptions;
using StreakTracker.API.Options;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.API.Services;

/// <inheritdoc cref="IGitHubAppService" />
public class GitHubAppService : IGitHubAppService
{
    /// <summary>GitHub, App JWT'lerinde en fazla 10 dakikalik omur kabul eder; guvenli tarafta kaliyoruz.</summary>
    private static readonly TimeSpan JwtLifetime = TimeSpan.FromMinutes(9);

    private readonly GitHubAppOptions _options;
    private readonly GitHubOptions _gitHubOptions;
    private readonly ILogger<GitHubAppService> _logger;

    public GitHubAppService(
        IOptions<GitHubAppOptions> options,
        IOptions<GitHubOptions> gitHubOptions,
        ILogger<GitHubAppService> logger)
    {
        _options = options.Value;
        _gitHubOptions = gitHubOptions.Value;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    public string InstallationUrl => _options.InstallationUrl;

    public async Task<long?> GetInstallationIdAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var appClient = CreateAppClient();

        try
        {
            var installation = await appClient.GitHubApps.GetUserInstallationForCurrent(username);
            return installation?.Id;
        }
        catch (NotFoundException)
        {
            // Kullanici App'i henuz kurmamis; bu bir hata degil, beklenen bir durum.
            _logger.LogInformation("GitHub App kurulumu bulunamadi: {Username}", username);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GitHub App kurulumu sorgulanamadi: {Username}", username);
            throw new GitHubServiceException("GitHub App kurulum bilgisi alinamadi.", ex);
        }
    }

    public async Task SendNotificationCommentAsync(
        long installationId,
        string username,
        string repositoryName,
        int issueNumber,
        string message,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        try
        {
            var installationClient = await CreateInstallationClientAsync(installationId);

            await installationClient.Issue.Comment.Create(username, repositoryName, issueNumber, message);

            _logger.LogInformation(
                "Bildirim yorumu bot kimligiyle gonderildi. Kullanici: {Username}, Issue: #{Number}",
                username, issueNumber);
        }
        catch (RateLimitExceededException ex)
        {
            _logger.LogWarning("GitHub App rate-limit asildi. Sifirlanma: {Reset}", ex.Reset);

            throw new GitHubServiceException("GitHub istek siniri asildi.", ex)
            {
                IsRateLimited = true,
                RateLimitResetsAt = ex.Reset
            };
        }
        catch (NotFoundException ex)
        {
            // Kurulum kaldirilmis veya repo erisimi verilmemis olabilir.
            _logger.LogWarning(ex,
                "Bot bildirim yorumunu atamadi; App kurulumu veya repo erisimi kaldirilmis olabilir. Kullanici: {Username}",
                username);

            throw new GitHubServiceException(
                "Bildirim gonderilemedi. GitHub App kurulumunun bildirim reposuna erisimi olduğundan emin olun.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bot bildirim yorumu gonderilemedi. Kullanici: {Username}", username);
            throw new GitHubServiceException("Bildirim yorumu gonderilemedi.", ex);
        }
    }

    // -----------------------------------------------------------------------
    // Yardimcilar
    // -----------------------------------------------------------------------

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new GitHubServiceException(
                "GitHub App yapilandirilmamis. Bildirimlerin calisabilmesi icin " +
                "GitHubApp:AppId ve GitHubApp:PrivateKey (veya PrivateKeyPath) tanimlanmalidir.");
        }
    }

    /// <summary>
    /// App'in kendi kimligiyle (JWT) yetkilendirilmis istemci olusturur.
    /// Bu istemci yalnizca App seviyesindeki islemler icin kullanilir.
    /// </summary>
    private GitHubClient CreateAppClient()
    {
        return new GitHubClient(new Octokit.ProductHeaderValue(_gitHubOptions.ProductHeaderName))
        {
            Credentials = new Credentials(GenerateAppJwt(), AuthenticationType.Bearer)
        };
    }

    /// <summary>
    /// Belirli bir kurulum icin gecici access token alir ve o token ile
    /// yetkilendirilmis istemci dondurur. Yapilan islemler <c>uygulamaadi[bot]</c>
    /// kimligiyle gorunur - bildirimlerin uretilmesini saglayan sey budur.
    /// </summary>
    private async Task<GitHubClient> CreateInstallationClientAsync(long installationId)
    {
        var appClient = CreateAppClient();
        var accessToken = await appClient.GitHubApps.CreateInstallationToken(installationId);

        return new GitHubClient(new Octokit.ProductHeaderValue(_gitHubOptions.ProductHeaderName))
        {
            Credentials = new Credentials(accessToken.Token)
        };
    }

    /// <summary>
    /// GitHub App'in private key'i ile imzalanmis, kisa omurlu bir JWT uretir (RS256).
    /// </summary>
    private string GenerateAppJwt()
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(_options.ResolvePrivateKey());

        var signingCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
        {
            // Imzalayici onbellege alinirsa RSA nesnesi serbest birakildiktan sonra
            // yeniden kullanilmaya calisilir; bunu engelliyoruz.
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };

        var now = DateTime.UtcNow;

        // Saat kaymalarina karsi 60 saniye geriden basliyoruz (GitHub'in onerisi).
        var issuedAt = now.AddSeconds(-60);

        // GitHub, App JWT'lerinde "iat" claim'ini zorunlu tutar. JwtSecurityToken
        // bu claim'i kendiliginden eklemedigi icin elle veriyoruz; aksi halde GitHub
        // "Missing 'issued at' claim ('iat') in assertion" hatasiyla 401 doner.
        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Iat,
                EpochTime.GetIntDate(issuedAt).ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _options.AppId,
            audience: null,
            claims: claims,
            notBefore: issuedAt,
            expires: now.Add(JwtLifetime),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
