using Microsoft.AspNetCore.Authorization;
using MonopolyServer.Services;

namespace MonopolyServer.Routes;


public class GameRoute
{
    private readonly GameService _gameService;
    public GameRoute(GameService gameService)
    {
        _gameService = gameService;
    }
    public void Map(WebApplication app)
    {

        app.MapPost("/game/create", [Authorize] () =>
        {
            Guid gameId = _gameService.CreateNewGame();
            return gameId;
        })
           .WithSummary("Get Game")
           .WithDescription("This endpoint returns a game message.");

        app.MapPost("/game/verify", (string gameId) =>
        {
            try
            {
                var game = _gameService.GetGame(Guid.Parse(gameId));
                return Results.Ok(game.GameId);
            }
            catch (InvalidOperationException e)
            {
                return Results.NotFound(e.Message);
            }
        });

    }
}