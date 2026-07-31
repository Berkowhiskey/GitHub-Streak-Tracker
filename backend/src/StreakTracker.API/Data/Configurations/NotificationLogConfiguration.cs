using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreakTracker.API.Entities;

namespace StreakTracker.API.Data.Configurations;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("notification_logs");

        builder.HasKey(n => n.Id);

        // Enum'i int yerine metin olarak saklariz; log tablosu dogrudan SQL ile incelendiginde okunur kalir.
        builder.Property(n => n.Channel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(n => n.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(n => n.IsSuccess)
            .HasDefaultValue(false);

        builder.Property(n => n.IsTest)
            .HasDefaultValue(false);

        // Kilometre tasi kutlamalarinin mukerrer gonderilmedigini kontrol eden sorgu.
        builder.HasIndex(n => new { n.UserId, n.MilestoneDay });

        // "Bu kullaniciya bugun bildirim gonderdik mi?" sorgusu icin.
        builder.HasIndex(n => new { n.UserId, n.SentAt });
    }
}
