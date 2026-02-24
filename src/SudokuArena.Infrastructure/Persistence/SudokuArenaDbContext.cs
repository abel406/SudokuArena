using Microsoft.EntityFrameworkCore;
using SudokuArena.Infrastructure.Persistence.Entities;

namespace SudokuArena.Infrastructure.Persistence;

public sealed class SudokuArenaDbContext(DbContextOptions<SudokuArenaDbContext> options) : DbContext(options)
{
    public DbSet<MatchEntity> Matches => Set<MatchEntity>();

    public DbSet<OutboxEventEntity> OutboxEvents => Set<OutboxEventEntity>();

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
    }
}
