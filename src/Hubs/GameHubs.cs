using Microsoft.AspNetCore.SignalR;
using MonopolyServer.Services;

namespace MonopolyServer.GameHubs
{
    public interface IResponse
    {
        Task CreateGameResponse(Guid newGameGuid);
        Task PlayerIdAssignmentResponse(Guid playerId);
        Task JoinGameResponse(Player player); 
        Task StartGameResponse(GameState game);
        Task DiceRolledResponse(Guid playerId, int roll1, int roll2, int totalRoll);
        Task PlayerJailResponse(Guid playerId);
        Task GameEnded(Guid gameId);
    }

    public class GameHubs : Hub<IResponse>
    {
        private readonly GameService _gameService;
        public GameHubs(GameService gameService)
        {
            _gameService = gameService;
        }

        public override async Task OnConnectedAsync()
        {
        
            Console.WriteLine($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}, Exception: {exception?.Message}");
        
        
            await base.OnDisconnectedAsync(exception);
        }

        public async Task CreateGame()
        {
            (Guid gameGuid, GameState newGame) = await _gameService.CreateNewGame();
        
            await Clients.Caller.CreateGameResponse(gameGuid);
        }

        public async Task JoinGame(Guid gameGuid, string playerName)
        {
        
            await Groups.AddToGroupAsync(Context.ConnectionId, gameGuid.ToString());
        
            Player newPlayer = await _gameService.AddPlayerToGame(gameGuid, playerName);

            await Clients.Caller.PlayerIdAssignmentResponse(newPlayer.Id);

        }

        public async Task StartGame(Guid gameGuid)
        {
            await _gameService.StartGame(gameGuid);
        }

        public async Task RollDice(Guid gameGuid, Guid playerGuid)
        {
            await _gameService.ProcessDiceRoll(gameGuid, playerGuid);
        }

    }
}