using Microsoft.AspNetCore.SignalR;
using MonopolyServer.Services;
using MonopolyServer.Utils;

namespace MonopolyServer.GameHubs
{
    public interface IResponse
    {
        #region Game Control Response
        Task CreateGameResponse(Guid newGameGuid);
        Task PlayerIdAssignmentResponse(Guid playerGuid, GameState game);
        Task JoinGameResponse(Guid gameGuid, List<Player> players);
        Task StartGameResponse(Guid gameGuid, List<Player> newPlayerOrder);
        Task GameEnded(Guid gameGuid);
        #endregion

        #region Game Event Response
        Task DiceRolledResponse(Guid gameGuid, Guid playerGuid, RollResult rollResult);
        Task EndTurnResponse(Guid gameGuid, int nextPlayerIndex);
        Task PlayerMoneyChangeResponse(Guid gameGuid, Guid playerGuid, decimal newMoneyValue);
        Task PropertyBoughtResponse(Guid gameId, Guid buyerId, Guid propertyGuid, decimal playerRemainingMoney);
        #endregion
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
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}, Exception: {exception?.ToString()}");


            await base.OnDisconnectedAsync(exception);
        }

        // TODO: Remove and make this one a rest api
        public async Task CreateGame()
        {
            (Guid gameGuid, GameState newGame) = await _gameService.CreateNewGame();

            await Clients.Caller.CreateGameResponse(gameGuid);
        }

        #region Game Control
        public async Task JoinGame(Guid gameGuid, string playerName)
        {

            await Groups.AddToGroupAsync(Context.ConnectionId, gameGuid.ToString());

            Player newPlayer = await _gameService.AddPlayerToGame(gameGuid, playerName);

            GameState joinedGame = _gameService.GetGame(gameGuid);

            await Clients.Caller.PlayerIdAssignmentResponse(newPlayer.Id, joinedGame);
        }

        public async Task StartGame(Guid gameGuid)
        {
            await _gameService.StartGame(gameGuid);
        }
        #endregion

        // Probably have to rewrite playerGuid param to be auth
        #region Game Event
        public async Task RollDice(Guid gameGuid, Guid playerGuid)
        {
            Console.Write("Rolling dice...");
            await _gameService.ProcessDiceRoll(gameGuid, playerGuid);
        }

        public async Task BuyProperty(Guid gameGuid, Guid playerGuid)
        {
            Console.Write("Buying property...");
            await _gameService.BuyProperty(gameGuid, playerGuid);
        }

        public async Task UpgradeProperty(Guid gameGuid, Guid playerGuid, Guid spaceGuid)
        {
            await _gameService.UpgradeProperty(gameGuid, playerGuid, spaceGuid);
        }

        
        public async Task EndTurn(Guid gameGuid, Guid playerGuid)
        {
            Console.Write("Ending turn...");
            await _gameService.EndTurn(gameGuid, playerGuid);
        }


        #endregion

    }
}