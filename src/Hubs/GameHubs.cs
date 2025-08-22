using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MonopolyServer.Models;
using MonopolyServer.Services;
using MonopolyServer.Utils;

namespace MonopolyServer.GameHubs;

public interface IResponse
{
    #region Game Control Response
    Task JoinGameResponse(Guid gameId, List<Player> players);
    Task StartGameResponse(Guid gameId, List<Player> newPlayerOrder);
    Task GameEnded(Guid gameId);
    Task GameOverResponse(Guid gameId, Guid winningPlayerId);
    Task SyncGameResponse(GameState game);

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
    Task PropertyDowngradeResponse(Guid gameId, Guid propertyId, List<TransactionInfo> transactions);
    Task PropertyUpgradeResponse(Guid gameId, Guid propertyId, List<TransactionInfo> transactions);
    Task PropertyMortgagedResponse(Guid gameId, Guid propertyId, List<TransactionInfo> transactions);
    Task PropertyUnmortgagedResponse(Guid gameId, Guid propertyId, List<TransactionInfo> transactions);

    // Trading stuff
    Task InitiateTradeResponse(Guid gameId, Trade trade);
    Task AcceptTradeResponse(Guid gameId, Trade trade, List<TransactionInfo> transactions);
    Task RejectTradeResponse(Guid gameId, Guid tradeId);
    Task CancelTradeResponse(Guid gameId, Guid tradeId);
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

    private Guid GetPlayerId()
    {
        if (Context.User == null) throw new Exception("No user found");
        var player = Context.User.Claims.FirstOrDefault(e => e.Type == ClaimTypes.Sid) ?? throw new Exception("No user id found");
        return Guid.Parse(player.Value);
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
    public async Task JoinGame(Guid gameId, string playerName, string hexColor)
    {
        var newPlayerId = GetPlayerId();
        await _gameService.AddPlayerToGame(gameId, playerName, hexColor, newPlayerId);

    }
    // All game is spectate by default
    public async Task SpectateGame(Guid gameId)
    {
        GameState game = _gameService.GetGame(gameId);

        await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());
    }
    public async Task SyncGame(Guid gameId)
    {
        GameState game = _gameService.GetGame(gameId);

        await Clients.Caller.SyncGameResponse(game);
    }

    public async Task StartGame(Guid gameId)
    {
        await _gameService.StartGame(gameId);
    }
    #endregion

    // Probably have to rewrite playerId param to be auth
    #region Game Event
    public async Task RollDice(Guid gameId)
    {
        var playerId = GetPlayerId();
        await _gameService.ProcessDiceRoll(gameId, playerId);
    }
    public async Task EndTurn(Guid gameId)
    {
        var playerId = GetPlayerId();
        await _gameService.EndTurn(gameId, playerId);
    }
    public async Task DeclareBankcruptcy(Guid gameId)
    {
        var playerId = GetPlayerId();
        await _gameService.DeclareBankcruptcy(gameId, playerId);
    }
    public async Task UseGetOutOfJailCard(Guid gameId)
    {
        var playerId = GetPlayerId();
        await _gameService.UseGetOutOfJailCard(gameId, playerId);
    }
    public async Task PayToGetOutOfJail(Guid gameId)
    {
        var playerId = GetPlayerId();
        await _gameService.PayToGetOutOfJail(gameId, playerId);
    }

    // Property stuff
    public async Task BuyProperty(Guid gameId)
    {
        var playerId = GetPlayerId();
        await _gameService.BuyProperty(gameId, playerId);
    }
    public async Task SellProperty(Guid gameId, Guid propertyId)
    {
        var playerId = GetPlayerId();
        await _gameService.SellProperty(gameId, playerId, propertyId);
    }

    public async Task UpgradeProperty(Guid gameId, Guid propertyId)
    {
        var playerId = GetPlayerId();
        await _gameService.UpgradeProperty(gameId, playerId, propertyId);
    }
    public async Task DowngradeProperty(Guid gameId, Guid propertyId)
    {
        var playerId = GetPlayerId();
        await _gameService.DowngradeProperty(gameId, playerId, propertyId);
    }
    public async Task MortgageProperty(Guid gameId, Guid propertyId)
    {
        var playerId = GetPlayerId();
        await _gameService.MortgageProperty(gameId, playerId, propertyId);
    }
    public async Task UnmortgageProperty(Guid gameId, Guid propertyId)
    {
        var playerId = GetPlayerId();
        await _gameService.UnmortgageProperty(gameId, playerId, propertyId);
    }

    // Trade stuff     
    public async Task InitiateTrade(Guid gameId, Guid recipientId, List<Guid> propertyOffer, List<Guid> propertyCounterOffer, decimal moneyFromInitiator, decimal moneyFromRecipient)
    {
        var initiatorId = GetPlayerId();
        await _gameService.InitiateTrade(gameId, initiatorId, recipientId, propertyOffer, propertyCounterOffer, moneyFromInitiator, moneyFromRecipient);
    }
    public async Task NegotiateTrade(Guid gameId,Guid tradeId, List<Guid> propertyOffer, List<Guid> propertyCounterOffer, decimal moneyFromInitiator, decimal moneyFromRecipient)
    {
        var negotiatorId = GetPlayerId();
        await _gameService.NegotiateTrade(gameId, negotiatorId, tradeId, propertyOffer, propertyCounterOffer, moneyFromInitiator, moneyFromRecipient);
    }
    public async Task AcceptTrade(Guid gameId, Guid tradeId)
    {
        var recipientId = GetPlayerId();
        await _gameService.AcceptTrade(gameId, recipientId, tradeId);
    }
    public async Task RejectTrade(Guid gameId, Guid tradeId)
    {
        var recipientId = GetPlayerId();
        await _gameService.RejectTrade(gameId, recipientId, tradeId);
    }
    public async Task CancelTrade(Guid gameId, Guid tradeId)
    {
        var initiatorId = GetPlayerId();
        await _gameService.CancelTrade(gameId, initiatorId, tradeId);
    }
    #endregion

}
