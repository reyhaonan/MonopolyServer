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

        app.MapPost("/game/create", async () =>
        {
            var (game, _) = await _gameService.CreateNewGame();
            return game.ToString();
        })
           .WithSummary("Get Game")
           .WithDescription("This endpoint returns a game message.");

    }
}