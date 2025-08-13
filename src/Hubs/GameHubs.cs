using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MonopolyServer.Models;
using MonopolyServer.Services;
using MonopolyServer.Utils;

namespace MonopolyServer.GameHubs;

public interface IResponse
{
    #region Game Control Response
    Task CreateGameResponse(Guid newGameId);
    Task PlayerIdAssignmentResponse(Guid playerId);
    Task JoinGameResponse(Guid gameId, List<Player> players);
    Task StartGameResponse(Guid gameId, List<Player> newPlayerOrder);
    Task GameEnded(Guid gameId);
    Task GameOverResponse(Guid gameId, Guid winningPlayerId);
    Task SpectateGameResponse(GameState game);

    #endregion

    #region Game Event Response
    Task DiceRolledResponse(Guid gameId, Guid playerId, RollResult rollResult);
    Task EndTurnResponse(Guid gameId, int nextPlayerIndex);
    Task DeclareBankcruptcyResponse(Guid gameId, Guid removedPlayerId, int nextPlayerIndex);
    Task PayToGetOutOfJailResponse(Guid gameId, Guid playerId, List<TransactionInfo> transactions);
    Task UseGetOutOfJailCardResponse(Guid gameId, Guid playerId);



    // Property stuff
    Task PropertyBoughtResponse(Guid gameId, Guid buyerId, Guid propertyId, List<TransactionInfo> transactions);
    Task PropertySoldResponse(Guid gameId, Guid buyerId, Guid propertyId, List<TransactionInfo> transactions);
    Task PropertyDowngradeResponse(Guid gameId, Guid buyerId, Guid propertyId, List<TransactionInfo> transactions);
    Task PropertyUpgradeResponse(Guid gameId, Guid buyerId, Guid propertyId, List<TransactionInfo> transactions);
    Task PropertyMortgagedResponse(Guid gameId, Guid buyerId, Guid propertyId, List<TransactionInfo> transactions);
    Task PropertyUnmortgagedResponse(Guid gameId, Guid buyerId, Guid propertyId, List<TransactionInfo> transactions);

    // Trading stuff
    Task InitiateTradeResponse(Guid gameId, Trade trade);
    Task AcceptTradeResponse(Guid gameId, Guid tradeId, List<TransactionInfo> transactions);
    Task RejectTradeResponse(Guid gameId, Guid tradeId);
    Task NegotiateTradeResponse(Guid gameId, Trade trade);
    #endregion
}

[Authorize]
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
    // Use 
    public async Task JoinGame(Guid gameId, string playerName, Guid newPlayerGuid)
    {

        await _gameService.AddPlayerToGame(gameId, playerName, newPlayerGuid);

    }
    // All game is spectate by default
    public async Task SpectateGame(Guid gameId)
    {

        await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());

        GameState game = _gameService.GetGame(gameId);

        await Clients.Caller.SpectateGameResponse(game);
    }

    public async Task StartGame(Guid gameId)
    {
        await _gameService.StartGame(gameId);
    }
    #endregion

    // Probably have to rewrite playerId param to be auth
    #region Game Event
    public async Task RollDice(Guid gameId, Guid playerId)
    {
        await _gameService.ProcessDiceRoll(gameId, playerId);
    }
    public async Task EndTurn(Guid gameId, Guid playerId)
    {
        await _gameService.EndTurn(gameId, playerId);
    }
    public async Task DeclareBankcruptcy(Guid gameId, Guid playerId)
    {
        await _gameService.DeclareBankcruptcy(gameId, playerId);
    }
    public async Task UseGetOutOfJailCard(Guid gameId, Guid playerId)
    {
        await _gameService.UseGetOutOfJailCard(gameId, playerId);
    }
    public async Task PayToGetOutOfJail(Guid gameId, Guid playerId)
    {
        await _gameService.PayToGetOutOfJail(gameId, playerId);
    }

    // Property stuff
    public async Task BuyProperty(Guid gameId, Guid playerId)
    {
        await _gameService.BuyProperty(gameId, playerId);
    }
    public async Task SellProperty(Guid gameId, Guid playerId, Guid propertyId)
    {
        await _gameService.SellProperty(gameId, playerId, propertyId);
    }

    public async Task UpgradeProperty(Guid gameId, Guid playerId, Guid propertyId)
    {
        await _gameService.UpgradeProperty(gameId, playerId, propertyId);
    }
    public async Task DowngradeProperty(Guid gameId, Guid playerId, Guid propertyId)
    {
        await _gameService.DowngradeProperty(gameId, playerId, propertyId);
    }
    public async Task MortgageProperty(Guid gameId, Guid playerId, Guid propertyId)
    {
        await _gameService.MortgageProperty(gameId, playerId, propertyId);
    }
    public async Task UnmortgageProperty(Guid gameId, Guid playerId, Guid propertyId)
    {
        await _gameService.UnmortgageProperty(gameId, playerId, propertyId);
    }

    // Trade stuff     
    public async Task InitiateTrade(Guid gameId, Guid initiatorId, Guid recipientId, List<Guid> propertyOffer, List<Guid> propertyCounterOffer, decimal moneyFromInitiator, decimal moneyFromRecipient)
    {
        await _gameService.InitiateTrade(gameId, initiatorId, recipientId, propertyOffer, propertyCounterOffer, moneyFromInitiator, moneyFromRecipient);
    }
    public async Task AcceptTrade(Guid gameId, Guid approvalId, Guid tradeId)
    {
        await _gameService.AcceptTrade(gameId, approvalId, tradeId);
    }
    public async Task RejectTrade(Guid gameId, Guid approvalId, Guid tradeId)
    {
        await _gameService.RejectTrade(gameId, approvalId, tradeId);
    }
    #endregion

}
