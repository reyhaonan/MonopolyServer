using Microsoft.AspNetCore.SignalR;
using MonopolyServer.Models;
using MonopolyServer.Services;
using MonopolyServer.Utils;

namespace MonopolyServer.GameHubs;

public interface IResponse
{
    #region Game Control Response
    Task CreateGameResponse(Guid newGameGuid);
    Task PlayerIdAssignmentResponse(Guid playerGuid, GameState game);
    Task JoinGameResponse(Guid gameGuid, List<Player> players);
    Task StartGameResponse(Guid gameGuid, List<Player> newPlayerOrder);
    Task GameEnded(Guid gameGuid);
    Task GameOverResponse(Guid gameGuid, Guid winningPlayerGuid);
    #endregion

    #region Game Event Response
    Task DiceRolledResponse(Guid gameGuid, Guid playerGuid, RollResult rollResult);
    Task EndTurnResponse(Guid gameGuid, int nextPlayerIndex);
    Task DeclareBankcruptcyResponse(Guid gameGuid, Guid removedPlayerGuid, int nextPlayerIndex);


    // Property stuff
    Task PropertyBoughtResponse(Guid gameId, Guid buyerId, Guid propertyGuid, List<TransactionInfo> transactions);
    Task PropertySoldResponse(Guid gameId, Guid buyerId, Guid propertyGuid, List<TransactionInfo> transactions);
    Task PropertyDowngradeResponse(Guid gameId, Guid buyerId, Guid propertyGuid, List<TransactionInfo> transactions);
    Task PropertyUpgradeResponse(Guid gameId, Guid buyerId, Guid propertyGuid, List<TransactionInfo> transactions);
    Task PropertyMortgagedResponse(Guid gameId, Guid buyerId, Guid propertyGuid, List<TransactionInfo> transactions);
    Task PropertyUnmortgagedResponse(Guid gameId, Guid buyerId, Guid propertyGuid, List<TransactionInfo> transactions);

    // Trading stuff
    Task InitiateTradeResponse(Guid gameId, Trade trade);
    Task AcceptTradeResponse(Guid gameId, Guid tradeGuid, List<TransactionInfo> transactions);
    Task RejectTradeResponse(Guid gameId, Guid tradeGuid);
    #endregion
}

public class GameHubs : Hub<IResponse>
{
    private readonly GameService _gameService;

    private readonly ILogger<GameHubs> _logger;
    public GameHubs(GameService gameService, ILogger<GameHubs> logger)
    {
        _gameService = gameService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {

        _logger.LogInformation($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}, Exception: {exception?.ToString()}");


        await base.OnDisconnectedAsync(exception);
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
        await _gameService.ProcessDiceRoll(gameGuid, playerGuid);
    }
    public async Task EndTurn(Guid gameGuid, Guid playerGuid)
    {
        await _gameService.EndTurn(gameGuid, playerGuid);
    }
    public async Task DeclareBankcruptcy(Guid gameGuid, Guid playerGuid)
    {
        await _gameService.DeclareBankcruptcy(gameGuid, playerGuid);
    }

    // Property stuff
    public async Task BuyProperty(Guid gameGuid, Guid playerGuid)
    {
        await _gameService.BuyProperty(gameGuid, playerGuid);
    }
    public async Task SellProperty(Guid gameGuid, Guid playerGuid, Guid propertyGuid)
    {
        await _gameService.SellProperty(gameGuid, playerGuid, propertyGuid);
    }

    public async Task UpgradeProperty(Guid gameGuid, Guid playerGuid, Guid propertyGuid)
    {
        await _gameService.UpgradeProperty(gameGuid, playerGuid, propertyGuid);
    }
    public async Task DowngradeProperty(Guid gameGuid, Guid playerGuid, Guid propertyGuid)
    {
        await _gameService.DowngradeProperty(gameGuid, playerGuid, propertyGuid);
    }
    public async Task MortgageProperty(Guid gameGuid, Guid playerGuid, Guid propertyGuid)
    {
        await _gameService.MortgageProperty(gameGuid, playerGuid, propertyGuid);
    }
    public async Task UnmortgageProperty(Guid gameGuid, Guid playerGuid, Guid propertyGuid)
    {
        await _gameService.UnmortgageProperty(gameGuid, playerGuid, propertyGuid);
    }

    public async Task InitiateTrade(Guid gameGuid, Guid initiatorGuid, Guid recipientGuid, List<Guid> propertyOffer, List<Guid> propertyCounterOffer, decimal moneyFromInitiator, decimal moneyFromRecipient)
    {
        await _gameService.InitiateTrade(gameGuid, initiatorGuid, recipientGuid, propertyOffer, propertyCounterOffer, moneyFromInitiator, moneyFromRecipient);
    }
    public async Task AcceptTrade(Guid gameGuid, Guid approvalId, Guid tradeGuid)
    {
        await _gameService.AcceptTrade(gameGuid, approvalId, tradeGuid);
    }
    public async Task RejectTrade(Guid gameGuid, Guid approvalId, Guid tradeGuid)
    {
        await _gameService.RejectTrade(gameGuid, approvalId, tradeGuid);
    }
    // Trade stuff     
    #endregion

}
