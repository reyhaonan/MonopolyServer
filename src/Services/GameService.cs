using System.Collections.Concurrent;
using System.Transactions;
using MonopolyServer.Enums;
using MonopolyServer.Models;

namespace MonopolyServer.Services;

public class GameService
{
    private static readonly ConcurrentDictionary<Guid, GameState> _activeGames = new();
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<GameService> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public GameService(IEventPublisher eventPublisher, ILogger<GameService> logger, ILoggerFactory loggerFactory)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public GameState GetGame(Guid gameId)
    {
        GameState? game;
        if (!_activeGames.TryGetValue(gameId, out game))
        {
            throw new InvalidOperationException($"Game with GUID '{gameId}' not found in On Going Games");
        }
        return game;
    }

    public Guid CreateNewGame()
    {
        var newGame = new GameState(_loggerFactory.CreateLogger<GameState>());
        Guid gameId = newGame.GameId;
        _activeGames.TryAdd(gameId, newGame);

        return gameId;
    }


    public async Task<Player> AddPlayerToGame(Guid gameId, string playerName, Guid newPlayerId)
    {
        GameState game = GetGame(gameId);
        if (game.CurrentPhase != GamePhase.WaitingForPlayers) throw new InvalidOperationException("Game is already started");
        if (game.ActivePlayers.Any(p => p.Id == newPlayerId)) throw new InvalidOperationException("Player already joined the game");
        Player newPlayer = new Player(playerName, newPlayerId);
        game.AddPlayer(newPlayer);

        await _eventPublisher.PublishGameControlEvent("PlayerJoined", gameId, new { Players = game.ActivePlayers });

        return newPlayer;
    }

    

    public async Task StartGame(Guid gameId)
    {
        GameState game = GetGame(gameId);
        var newPlayerOrder = game.StartGame();

        await _eventPublisher.PublishGameControlEvent("GameStart", gameId, new { NewPlayerOrder = newPlayerOrder });
    }

    
    public async Task ProcessDiceRoll(Guid gameId, Guid playerId)
    {
        GameState game = GetGame(gameId);

        if (!playerId.Equals(game.GetCurrentPlayer().Id)) throw new InvalidOperationException($"Player {playerId} are not permitted for this action.  current active are: {game.GetCurrentPlayer().Id}");

        var result = game.RollDice();

        _logger.LogInformation($"Transaction result ${result.Transaction.Count}");

        await _eventPublisher.PublishGameActionEvent("DiceRolled", gameId, new
        {
            PlayerId = playerId,
            RollResult = result
        });
    }



    
    public async Task EndTurn(Guid gameId, Guid playerId)
    {
        GameState game = GetGame(gameId);
        if (!playerId.Equals(game.GetCurrentPlayer().Id)) throw new InvalidOperationException($"Player {playerId} are not permitted for this action.");

        int nextPlayerIndex = game.EndTurn();

        await _eventPublisher.PublishGameActionEvent("EndTurn", gameId, new
        {
            NextPlayerIndex = nextPlayerIndex
        });
    }

    public async Task PayToGetOutOfJail(Guid gameId, Guid playerId)
    {

        GameState game = GetGame(gameId);
        if (!playerId.Equals(game.GetCurrentPlayer().Id)) throw new InvalidOperationException($"Player {playerId} are not permitted for this action.");

        var transactions = game.PayToGetOutOfJail();

        await _eventPublisher.PublishGameActionEvent("PayToGetOutOfJail", gameId, new
        {
            Transactions = transactions,
            PlayerId = playerId
        });

    }
    
    
    public async Task UseGetOutOfJailCard(Guid gameId, Guid playerId)
    {

        GameState game = GetGame(gameId);
        if (!playerId.Equals(game.GetCurrentPlayer().Id)) throw new InvalidOperationException($"Player {playerId} are not permitted for this action.");

        game.UseGetOutOfJailCard();

        await _eventPublisher.PublishGameActionEvent("UseGetOutOfJailCard", gameId, new
        {
            PlayerId = playerId
        });

    }

    public async Task DeclareBankcruptcy(Guid gameId, Guid playerId)
    {
        GameState game = GetGame(gameId);

        int nextPlayerIndex = game.DeclareBankcruptcy(playerId);

        await _eventPublisher.PublishGameActionEvent("DeclareBankcruptcy", gameId, new
        {
            RemovedPlayerId = playerId,
            NextPlayerIndex = nextPlayerIndex
        });

        if (game.ActivePlayers.Count == 1)
        {
            await _eventPublisher.PublishGameControlEvent("GameOver", gameId, new
            {
                WinningPlayerId = game.ActivePlayers.First().Id
            });
        }

    }
  
    
    public async Task BuyProperty(Guid gameId, Guid playerId)
    {
        GameState game = GetGame(gameId);
        Player currentPlayer = game.GetCurrentPlayer();
        if (!playerId.Equals(currentPlayer.Id)) throw new InvalidOperationException($"Player {playerId} are not permitted for this action.");

        var (propertyId, transactions) = game.BuyProperty();

        await _eventPublisher.PublishGameActionEvent("PropertyBought", gameId, new
        {
            PlayerId = playerId,
            PropertyId = propertyId,
            Transactions = transactions
        });
    }

    
    
    public async Task SellProperty(Guid gameId, Guid playerId, Guid propertyId)
    {
        GameState game = GetGame(gameId);
        Player currentPlayer = game.GetCurrentPlayer();
        if (!playerId.Equals(currentPlayer.Id)) throw new InvalidOperationException($"Player {playerId} are not permitted for this action.");

        var transactions = game.SellProperty(propertyId);

        await _eventPublisher.PublishGameActionEvent("PropertySold", gameId, new
        {
            PlayerId = playerId,
            PropertyId = propertyId,
            Transactions = transactions
        });
    }
    

    public async Task UpgradeProperty(Guid gameId, Guid playerId, Guid propertyId)
    {
        GameState game = GetGame(gameId);
        Player currentPlayer = game.GetCurrentPlayer();

        if (!playerId.Equals(currentPlayer.Id)) throw new InvalidOperationException($"Player {playerId} are not permitted for this action.");

        var transactions = game.UpgradeProperty(propertyId);

        await _eventPublisher.PublishGameActionEvent("PropertyUpgrade", gameId, new
        {
            PlayerId = playerId,
            PropertyId = propertyId,
            Transactions = transactions
        });

    }
    
    public async Task DowngradeProperty(Guid gameId, Guid playerId, Guid propertyId)
    {
        GameState game = GetGame(gameId);
        Player currentPlayer = game.GetCurrentPlayer();

        if (!playerId.Equals(currentPlayer.Id)) throw new InvalidOperationException($"Player {playerId} are not permitted for this action.");

        var transactions = game.DowngradeProperty(propertyId);

        await _eventPublisher.PublishGameActionEvent("PropertyDowngrade", gameId, new
        {
            PlayerId = playerId,
            PropertyId = propertyId,
            Transactions = transactions
        });

    }
    
    public async Task MortgageProperty(Guid gameId, Guid playerId, Guid propertyId)
    {
        GameState game = GetGame(gameId);
        Player currentPlayer = game.GetCurrentPlayer();

        if (!playerId.Equals(currentPlayer.Id)) throw new InvalidOperationException($"Player {playerId} are not permitted for this action.");

        var transactions = game.MortgageProperty(propertyId);

        await _eventPublisher.PublishGameActionEvent("PropertyMortgage", gameId, new
        {
            PlayerId = playerId,
            PropertyId = propertyId,
            Transactions = transactions
        });

    }
    
    public async Task UnmortgageProperty(Guid gameId, Guid playerId, Guid propertyId)
    {
        GameState game = GetGame(gameId);
        Player currentPlayer = game.GetCurrentPlayer();

        if (!playerId.Equals(currentPlayer.Id)) throw new InvalidOperationException($"Player {playerId} are not permitted for this action.");

        var transactions = game.UnmortgageProperty(propertyId);

        await _eventPublisher.PublishGameActionEvent("PropertyUnmortgage", gameId, new
        {
            PlayerId = playerId,
            PropertyId = propertyId,
            Transactions = transactions
        });

    }

    
    public async Task InitiateTrade(Guid gameId, Guid initiatorId, Guid recipientId, List<Guid> propertyOffer, List<Guid> propertyCounterOffer, decimal moneyFromInitiator, decimal moneyFromRecipient)
    {
        GameState game = GetGame(gameId);

        if(initiatorId.Equals(recipientId))throw new InvalidOperationException("Cannot trade with yourself");

        var trade = game.InitiateTrade(initiatorId, recipientId, propertyOffer, propertyCounterOffer, moneyFromInitiator, moneyFromRecipient);

        await _eventPublisher.PublishGameActionEvent("InitiateTrade", gameId, new
        {
            Trade = trade
        });

    }

    
    public async Task AcceptTrade(Guid gameId, Guid recipientId, Guid tradeId)
    {
        GameState game = GetGame(gameId);

        var (transactions, trade) = game.AcceptTrade(tradeId, recipientId);

        await _eventPublisher.PublishGameActionEvent("AcceptTrade", gameId, new
        {
            Trade = trade,
            Transactions = transactions
        });


    }
    
    
    public async Task RejectTrade(Guid gameId, Guid approvalId, Guid tradeId)
    {
        GameState game = GetGame(gameId);

        game.RejectTrade(tradeId, approvalId);

        await _eventPublisher.PublishGameActionEvent("RejectTrade", gameId, new
        {
            TradeId = tradeId
        });

    }
    public async Task CancelTrade(Guid gameId, Guid initiatorId, Guid tradeId)
    {
        GameState game = GetGame(gameId);

        game.CancelTrade(tradeId, initiatorId);

        await _eventPublisher.PublishGameActionEvent("CancelTrade", gameId, new
        {
            TradeId = tradeId
        });

    }

    public async Task NegotiateTrade(Guid gameId, Guid negotiatorId, Guid tradeId, List<Guid> propertyOffer, List<Guid> propertyCounterOffer, decimal moneyFromInitiator, decimal moneyFromRecipient)
    {
        GameState game = GetGame(gameId);

        Trade trade = game.NegotiateTrade(negotiatorId, tradeId, propertyOffer, propertyCounterOffer, moneyFromInitiator, moneyFromRecipient);

        await _eventPublisher.PublishGameActionEvent("NegotiateTrade", gameId, new
        {
            Trade = trade
        });
    }
    
    public void Dispose()
    {
        // If we need to dispose of any resources in the future
        // Currently, the IEventPublisher is responsible for disposing its own resources
    }
}
