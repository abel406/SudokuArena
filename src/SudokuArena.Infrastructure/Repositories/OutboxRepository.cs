using Microsoft.EntityFrameworkCore;
using SudokuArena.Application.Abstractions.Repositories;
using SudokuArena.Infrastructure.Persistence;
using SudokuArena.Infrastructure.Persistence.Entities;

namespace SudokuArena.Infrastructure.Repositories;

public sealed class OutboxRepository(SudokuArenaDbContext dbContext) : IOutboxRepository
{
    public async Task EnqueueAsync(string eventType, string payload, CancellationToken cancellationToken)
    {
        dbContext.OutboxEvents.Add(new OutboxEventEntity
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = payload,
            CreatedUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxEvent>> DequeuePendingAsync(int take, CancellationToken cancellationToken)
    {
        var pending = await dbContext.OutboxEvents
            .Where(x => x.SyncedUtc == null)
            .Take(take)
            .ToListAsync(cancellationToken);

        return pending
            .OrderBy(x => x.CreatedUtc)
            .Select(x => new OutboxEvent(x.Id, x.EventType, x.Payload, x.CreatedUtc))
            .ToList();
    }

    public async Task MarkAsSyncedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.OutboxEvents.SingleOrDefaultAsync(x => x.Id == eventId, cancellationToken);
        if (entity is null)
        {
            return;
        }

        entity.SyncedUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
