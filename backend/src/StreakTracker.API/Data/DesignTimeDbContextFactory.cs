using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StreakTracker.API.Services;

namespace StreakTracker.API.Data;

/// <summary>
/// "dotnet ef migrations add / database update" komutlarinin DbContext'i olusturabilmesi icin kullanilir.
/// Bu factory sayesinde EF Tools uygulamanin tamamini ayaga kaldirmaz; boylece
/// migration uretmek icin calisan bir veritabani veya Hangfire baglantisi gerekmez.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Migration uretimi icin gecerli bir baglanti dizesi sart degildir; saglayici bilgisi yeterlidir.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Host=localhost;Port=5434;Database=streaktracker;Username=streaktracker;Password=streaktracker_dev_2026";
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        // Migration uretimi sirasinda veri okunup yazilmaz; yine de DbContext'in
        // zorunlu bagimliligini karsilamak icin gecici bir koruyucu olusturulur.
        var tokenProtector = new TokenProtector(
            DataProtectionProvider.Create(nameof(StreakTracker)),
            NullLogger<TokenProtector>.Instance);

        return new AppDbContext(options, tokenProtector);
    }
}
