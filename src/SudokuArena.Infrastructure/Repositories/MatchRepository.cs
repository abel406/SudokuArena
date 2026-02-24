using Microsoft.EntityFrameworkCore;
using SudokuArena.Application.Abstractions.Repositories;
using SudokuArena.Domain.Models;
using SudokuArena.Infrastructure.Persistence;
using SudokuArena.Infrastructure.Persistence.Entities;

namespace SudokuArena.Infrastructure.Repositories;

public sealed class MatchRepository(SudokuArenaDbContext dbContext) : IMatchRepository
{
    public async Task SaveAsync(DuelMatch match, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Matches.SingleOrDefaultAsync(x => x.Id == match.Id, cancellationToken);
        if (entity is null)
        {
            entity = new MatchEntity
            {
                Id = match.Id
            };
            dbContext.Matches.Add(entity);
        }

        entity.HostPlayer = match.HostPlayer;
        entity.GuestPlayer = match.GuestPlayer;
        entity.Transport = (int)match.Transport;
        entity.InitialPuzzle = match.InitialPuzzle;
        entity.BoardState = match.Board.ToStateString();
        entity.CreatedUtc = match.CreatedUtc;
        entity.CompletedUtc = match.CompletedUtc;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DuelMatch?> GetByIdAsync(Guid matchId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Matches.SingleOrDefaultAsync(x => x.Id == matchId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var board = RehydrateBoard(entity.InitialPuzzle, entity.BoardState);
        var match = new DuelMatch(
            entity.Id,
            entity.HostPlayer,
            entity.GuestPlayer,
            (MatchTransport)entity.Transport,
            entity.InitialPuzzle,
            board,
            entity.CreatedUtc,
            entity.CompletedUtc);

        return match;
    }

    private static SudokuBoard RehydrateBoard(string initialPuzzle, string boardState)
    {
        var board = SudokuBoard.CreateFromString(initialPuzzle);
        var length = Math.Min(boardState.Length, initialPuzzle.Length);

        for (var i = 0; i < length; i++)
        {
            if (boardState[i] is '.' or '0')
            {
                continue;
            }

            if (initialPuzzle[i] is not ('0' or '.'))
            {
                continue;
            }

            var value = boardState[i] - '0';
            _ = board.TrySetCell(i, value, out _);
        }

        return board;
    }
}
