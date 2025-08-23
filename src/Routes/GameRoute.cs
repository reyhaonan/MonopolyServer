using Microsoft.AspNetCore.Authorization;
using MonopolyServer.Services;

namespace MonopolyServer.Routes;


public static class GameRoute
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/game");
        group.MapPost("/create", [Authorize] (GameManager gameManager) =>
        {
            Console.WriteLine("Creating game");
            Guid gameId = gameManager.CreateNewGame();
            return gameId;
        })
           .WithSummary("Get Game")
           .WithDescription("This endpoint returns a game message.");

        group.MapPost("/verify", (string gameId, GameManager gameManager) =>
        {
            try
            {
                var game = gameManager.GetGame(Guid.Parse(gameId));
                return Results.Ok(game.GameId);
            }
            catch (InvalidOperationException e)
            {
                return Results.NotFound(e.Message);
            }
        });

    }
}