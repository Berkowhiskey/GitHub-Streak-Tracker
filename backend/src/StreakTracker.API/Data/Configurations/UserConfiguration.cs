using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreakTracker.API.Entities;

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

        builder.Property(u => u.PreferredNotificationHourUtc)
            .HasDefaultValue(20);

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

        // Bildirim job'u "su saatte bildirim alacak aktif kullanicilar" sorgusunu bu index ile ceker.
        builder.HasIndex(u => new { u.IsActive, u.PreferredNotificationHourUtc });

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
