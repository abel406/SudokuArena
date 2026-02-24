namespace SudokuArena.Domain.Models;

public sealed class PlayerProfile
{
    public required string Email { get; init; }

    public required string Nickname { get; set; }

    public string CountryCode { get; set; } = "US";

    public int Elo { get; set; } = 1200;

    public string? AvatarUrl { get; set; }
}
