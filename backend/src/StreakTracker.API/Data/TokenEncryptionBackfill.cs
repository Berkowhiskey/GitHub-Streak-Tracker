using Microsoft.EntityFrameworkCore;
using StreakTracker.API.Services.Interfaces;

namespace StreakTracker.API.Data;

/// <summary>
/// Sifreleme devreye alinmadan once kaydedilmis duz metin access token'lari
/// bir kerelik olarak sifreler. Uygulama acilisinda calisir ve idempotenttir:
/// sifrelenecek kayit yoksa hicbir sey yapmaz.
/// </summary>
public static class TokenEncryptionBackfill
{
    /// <summary>
    /// GitHub access token'larinin bilinen onekleri.
    /// <para>
    /// Bir degerin cozulememesi tek basina "duz metin" anlamina GELMEZ; deger
    /// baska bir DataProtection anahtariyla sifrelenmis de olabilir (orn. anahtar
    /// klasoru degistiginde). Boyle bir degeri yeniden sifrelemek veriyi geri
    /// donulmez sekilde bozar. Bu yuzden yalnizca gercekten GitHub token'i
    /// gorunumundeki degerler sifrelenir.
    /// </para>
    /// </summary>
    private static readonly string[] GitHubTokenPrefixes =
        ["gho_", "ghp_", "ghu_", "ghs_", "ghr_", "github_pat_"];

    public static async Task RunAsync(
        AppDbContext dbContext,
        ITokenProtector tokenProtector,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        // Ham degeri okuyabilmek icin EF'in value converter'ini atlayip
        // dogrudan SQL kullaniriz; aksi halde her deger cozulmus olarak gelir.
        var plainTextUserIds = new List<Guid>();

        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;

        if (shouldCloseConnection)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """SELECT "Id", "AccessToken" FROM users""";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);
                var storedToken = reader.GetString(1);

                if (tokenProtector.IsProtected(storedToken))
                    continue;

                if (LooksLikePlainGitHubToken(storedToken))
                {
                    plainTextUserIds.Add(id);
                }
                else
                {
                    // Cozulemiyor ama duz metin GitHub token'ina da benzemiyor:
                    // buyuk olasilikla BASKA bir DataProtection anahtariyla sifrelenmis.
                    // Yeniden sifrelemek degeri kurtarilamaz hale getirir - dokunmuyoruz.
                    logger.LogError(
                        "Kullanici {UserId} icin access token cozulemedi ve duz metin GitHub token'ina benzemiyor. " +
                        "Deger baska bir DataProtection anahtariyla sifrelenmis olabilir; bozmamak icin dokunulmadi. " +
                        "Anahtar klasoru (App:DataProtectionKeysPath) dogru mu kontrol edin. " +
                        "Anahtarlar gercekten kaybolduysa kullanicinin yeniden giris yapmasi gerekir.",
                        id);
                }
            }
        }
        finally
        {
            if (shouldCloseConnection)
                await connection.CloseAsync();
        }

        if (plainTextUserIds.Count == 0)
        {
            logger.LogDebug("Sifrelenmemis access token bulunamadi; backfill atlandi.");
            return;
        }

        var users = await dbContext.Users
            .Where(u => plainTextUserIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            // Deger okunurken degismedi (duz metin oldugu gibi geldi); yalnizca
            // "degisti" olarak isaretleyip kaydetmek, value converter'in sifreleyerek
            // yeniden yazmasi icin yeterlidir.
            dbContext.Entry(user).Property(u => u.AccessToken).IsModified = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "{Count} adet duz metin access token sifrelendi (bir kerelik gecis islemi).",
            users.Count);
    }

    /// <summary>
    /// Degerin sifrelenmemis bir GitHub access token'i olup olmadigini bildirir.
    /// </summary>
    private static bool LooksLikePlainGitHubToken(string value) =>
        GitHubTokenPrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal));
}
