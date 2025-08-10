using Microsoft.AspNetCore.Authorization;
using MonopolyServer.Services;

namespace MonopolyServer.Routes;


public static class GameRoute
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/game");
        group.MapPost("/create", [Authorize] (GameService gameService) =>
        {
            Guid gameId = gameService.CreateNewGame();
            return gameId;
        })
           .WithSummary("Get Game")
           .WithDescription("This endpoint returns a game message.");

        group.MapPost("/verify", (string gameId, GameService gameService) =>
        {
            try
            {
                var game = gameService.GetGame(Guid.Parse(gameId));
                return Results.Ok(game.GameId);
            }
            catch (InvalidOperationException e)
            {
                return Results.NotFound(e.Message);
            }
        });

    }
}