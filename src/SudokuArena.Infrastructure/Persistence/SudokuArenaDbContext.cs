using Microsoft.EntityFrameworkCore;
using SudokuArena.Infrastructure.Persistence.Entities;

namespace SudokuArena.Infrastructure.Persistence;

public sealed class SudokuArenaDbContext(DbContextOptions<SudokuArenaDbContext> options) : DbContext(options)
{
    public DbSet<MatchEntity> Matches => Set<MatchEntity>();

    public DbSet<OutboxEventEntity> OutboxEvents => Set<OutboxEventEntity>();

    public DbSet<ThemeEntity> Themes => Set<ThemeEntity>();

    public DbSet<MediaAssetEntity> MediaAssets => Set<MediaAssetEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MatchEntity>(entity =>
        {
            entity.ToTable("matches");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.HostPlayer).HasMaxLength(256).IsRequired();
            entity.Property(x => x.GuestPlayer).HasMaxLength(256).IsRequired();
            entity.Property(x => x.InitialPuzzle).HasMaxLength(81).IsRequired();
            entity.Property(x => x.BoardState).HasMaxLength(81).IsRequired();
            entity.Property(x => x.Transport).IsRequired();
            entity.HasIndex(x => x.CreatedUtc);
        });

        modelBuilder.Entity<OutboxEventEntity>(entity =>
        {
            entity.ToTable("outbox_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Payload).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => x.SyncedUtc);
        });

        modelBuilder.Entity<ThemeEntity>(entity =>
        {
            entity.ToTable("themes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Version).IsRequired();
            entity.Property(x => x.IsPublished).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.Priority).IsRequired();
            entity.Property(x => x.TokensJson).IsRequired();
            entity.Property(x => x.AssetsJson).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => new { x.IsActive, x.IsPublished, x.Priority });
        });

        modelBuilder.Entity<MediaAssetEntity>(entity =>
        {
            entity.ToTable("media_assets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FileName).HasMaxLength(240).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.StoragePath).HasMaxLength(300).IsRequired();
            entity.Property(x => x.SizeBytes).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => x.CreatedUtc);
        });
    }
}
