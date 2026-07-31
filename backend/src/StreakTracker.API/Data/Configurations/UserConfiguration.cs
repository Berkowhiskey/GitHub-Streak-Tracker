using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreakTracker.API.Entities;
using StreakTracker.API.Enums;

namespace StreakTracker.API.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.GitHubId)
            .IsRequired();

        builder.Property(u => u.GitHubUsername)
            .IsRequired()
            .HasMaxLength(39); // GitHub kullanici adi ust siniri

        builder.Property(u => u.Email)
            .HasMaxLength(320);

        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(500);

        // Sifrelenmis token, duz metinden belirgin olcude uzundur (DataProtection zarfi + base64).
        builder.Property(u => u.AccessToken)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(u => u.PrivateNotificationRepoName)
            .HasMaxLength(100);

        builder.Property(u => u.PreferredNotificationHour)
            .HasDefaultValue(20);

        // IANA saat dilimi kimligi (orn. "America/Argentina/ComodRivadavia" en uzunlarindan).
        builder.Property(u => u.TimeZoneId)
            .IsRequired()
            .HasMaxLength(64)
            .HasDefaultValue("UTC");

        // Enum metin olarak saklanir; veritabani dogrudan incelendiginde okunur kalir.
        builder.Property(u => u.Language)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10)
            .HasDefaultValue(AppLanguage.Turkish);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);

        builder.Property(u => u.HasAcceptedTerms)
            .HasDefaultValue(false);

        // GitHub ID degismez kimliktir; ayni hesabin iki kez kaydolmasini engeller.
        builder.HasIndex(u => u.GitHubId)
            .IsUnique();

        // Rozet endpoint'i (/api/v1/badges/{username}.svg) bu kolon uzerinden arama yapar.
        builder.HasIndex(u => u.GitHubUsername)
            .IsUnique();

        // Bildirim job'u once aktif kullanicilari ceker; saat eslesmesi saat dilimi
        // basina degistigi icin (DST) bellekte yapilir.
        builder.HasIndex(u => new { u.IsActive, u.PreferredNotificationHour });

        builder.HasOne(u => u.Streak)
            .WithOne(s => s.User)
            .HasForeignKey<Streak>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.NotificationLogs)
            .WithOne(n => n.User)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
