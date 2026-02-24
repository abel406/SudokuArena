namespace SudokuArena.Infrastructure.Persistence.Entities;

public sealed class MatchEntity
{
    public Guid Id { get; set; }

    public string HostPlayer { get; set; } = string.Empty;

    public string GuestPlayer { get; set; } = string.Empty;

    public int Transport { get; set; }

    public string InitialPuzzle { get; set; } = string.Empty;

    public string BoardState { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset? CompletedUtc { get; set; }
}
