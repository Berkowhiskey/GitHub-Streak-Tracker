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

                if (!tokenProtector.IsProtected(storedToken))
                    plainTextUserIds.Add(id);
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
}
