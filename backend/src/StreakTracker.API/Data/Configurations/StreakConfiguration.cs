using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreakTracker.API.Entities;

namespace StreakTracker.API.Data.Configurations;

public class StreakConfiguration : IEntityTypeConfiguration<Streak>
{
    public void Configure(EntityTypeBuilder<Streak> builder)
    {
        builder.ToTable("streaks");

        builder.HasKey(s => s.Id);

        // Her kullanicinin tek bir streak kaydi olur (1-1).
        builder.HasIndex(s => s.UserId)
            .IsUnique();

        builder.Property(s => s.CurrentStreak)
            .HasDefaultValue(0);

        builder.Property(s => s.LongestStreak)
            .HasDefaultValue(0);

        builder.Property(s => s.HasCommittedToday)
            .HasDefaultValue(false);

        // DateOnly -> PostgreSQL "date"; saat/timezone karmasasi olmadan gun bazli karsilastirma saglar.
        builder.Property(s => s.LastCommitDate)
            .HasColumnType("date");
    }
}
