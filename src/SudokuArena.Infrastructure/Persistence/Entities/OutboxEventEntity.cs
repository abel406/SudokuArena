namespace SudokuArena.Infrastructure.Persistence.Entities;

public sealed class OutboxEventEntity
{
    public Guid Id { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset? SyncedUtc { get; set; }
}
