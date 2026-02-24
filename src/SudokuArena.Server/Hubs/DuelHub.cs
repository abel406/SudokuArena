using Microsoft.AspNetCore.SignalR;
using SudokuArena.Application.Abstractions.Services;
using SudokuArena.Application.Contracts;

namespace SudokuArena.Server.Hubs;

public sealed class DuelHub(IMatchService matchService) : Hub
{
    public Task JoinMatch(Guid matchId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, GroupName(matchId));
    }

    public async Task<MoveResponse> SubmitMove(MoveHubRequest request, CancellationToken cancellationToken)
    {
        var response = await matchService.SubmitMoveAsync(
            new MoveSubmission(request.MatchId, request.PlayerEmail, request.CellIndex, request.Value),
            cancellationToken);

        if (response.Accepted)
        {
            await Clients.Group(GroupName(request.MatchId)).SendAsync(
                "moveApplied",
                new
                {
                    request.MatchId,
                    request.PlayerEmail,
                    request.CellIndex,
                    request.Value,
                    response.BoardState,
                    response.Completed
                },
                cancellationToken);
        }

        return response;
    }

    private static string GroupName(Guid matchId) => $"match:{matchId:N}";
}

public sealed record MoveHubRequest(Guid MatchId, string PlayerEmail, int CellIndex, int? Value);
