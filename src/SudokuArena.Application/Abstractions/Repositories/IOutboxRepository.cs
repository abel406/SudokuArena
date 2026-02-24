namespace SudokuArena.Application.Abstractions.Repositories;

public interface IOutboxRepository
{
    Task EnqueueAsync(string eventType, string payload, CancellationToken cancellationToken);

    Task<IReadOnlyList<OutboxEvent>> DequeuePendingAsync(int take, CancellationToken cancellationToken);

    Task MarkAsSyncedAsync(Guid eventId, CancellationToken cancellationToken);
}

public sealed record OutboxEvent(Guid Id, string EventType, string Payload, DateTimeOffset CreatedUtc);
