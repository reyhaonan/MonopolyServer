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

        app.MapPost("/game/create", () =>
        {
            Guid gameId = _gameService.CreateNewGame();
            return gameId;
        })
           .WithSummary("Get Game")
           .WithDescription("This endpoint returns a game message.");

        app.MapPost("/game/verify", (Guid gameId) =>
        {
            var game = _gameService.GetGame(gameId) ?? throw new Exception("Game doesnt exist");
            return game.GameId;
        });

    }
}