using Microsoft.EntityFrameworkCore;
using StreakTracker.API.Entities;

namespace StreakTracker.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Streak> Streaks => Set<Streak>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Data/Configurations altindaki tum IEntityTypeConfiguration siniflarini otomatik uygular.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// CreatedAt / UpdatedAt alanlarini merkezi olarak doldurur; boylece her serviste
    /// tarih atamasi tekrar edilmez. Tum degerler UTC'dir (PostgreSQL timestamptz gereksinimi).
    /// </summary>
    private void ApplyTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            if (entry.State == EntityState.Added && entry.Metadata.FindProperty(nameof(User.CreatedAt)) is not null)
                entry.CurrentValues[nameof(User.CreatedAt)] = now;

            if (entry.Metadata.FindProperty(nameof(User.UpdatedAt)) is not null)
                entry.CurrentValues[nameof(User.UpdatedAt)] = now;
        }
    }
}
