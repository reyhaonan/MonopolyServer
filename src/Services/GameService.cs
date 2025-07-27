using System.Collections.Concurrent;
using MonopolyServer.Models;

namespace MonopolyServer.Services;

public class GameService : IDisposable
    {
        private static readonly ConcurrentDictionary<Guid, GameState> _activeGames = new();
        private readonly IEventPublisher _eventPublisher;

        public GameService(IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        public GameState GetGame(Guid gameGuid)
        {
            GameState? game;
            if (!_activeGames.TryGetValue(gameGuid, out game))
            {
                throw new InvalidOperationException($"Game with GUID '{gameGuid}' not found in On Going Games");
            }
            return game;
        }

        public async Task<(Guid gameGuid, GameState gameState)> CreateNewGame()
        {
            var newGame = new GameState();
            Guid gameGuid = newGame.GameId;
            _activeGames.TryAdd(gameGuid, newGame);

            await _eventPublisher.PublishGameControlEvent("GameCreated", gameGuid, new { Game = newGame });

            return (gameGuid, newGame);
        }
        

        public async Task<Player> AddPlayerToGame(Guid gameGuid, string playerName)
        {
            GameState game = GetGame(gameGuid);

            Player newPlayer = new Player(playerName);
            game.AddPlayer(newPlayer);

            await _eventPublisher.PublishGameControlEvent("PlayerJoined", gameGuid, new { Players = game.ActivePlayers });

            return newPlayer;
        }

        public async Task StartGame(Guid gameGuid)
        {
            GameState game = GetGame(gameGuid);
            var newPlayerOrder = game.StartGame();

            await _eventPublisher.PublishGameControlEvent("GameStart", gameGuid, new { NewPlayerOrder = newPlayerOrder });
        }

        public async Task ProcessDiceRoll(Guid gameGuid, Guid playerGuid)
        {
            GameState game = GetGame(gameGuid);
            foreach (var p in game.ActivePlayers)
            {
                Console.WriteLine(p.Id);
            }
            if (!playerGuid.Equals(game.GetCurrentPlayer().Id)) throw new InvalidOperationException($"Player {playerGuid} are not permitted for this action.  current active are: {game.GetCurrentPlayer().Id}");

            var result = game.RollDice();
            
            await _eventPublisher.PublishGameActionEvent("DiceRolled", gameGuid, new
            {
                PlayerId = playerGuid,
                RollResult = result
            });
        }

        public async Task BuyProperty(Guid gameGuid, Guid playerGuid)
        {
            GameState game = GetGame(gameGuid);
            Player currentPlayer = game.GetCurrentPlayer();
            if (!playerGuid.Equals(currentPlayer.Id)) throw new InvalidOperationException($"Player {playerGuid} are not permitted for this action.");

            var propertyGuid = game.BuyProperty();
            
            await _eventPublisher.PublishGameActionEvent("PropertyBought", gameGuid, new
            {
                PlayerId = playerGuid,
                PropertyGuid = propertyGuid,
                PlayerRemainingMoney = currentPlayer.Money
            });
        }
        public async Task SellProperty(Guid gameGuid, Guid playerGuid, Guid propertyGuid)
        {
            GameState game = GetGame(gameGuid);
            Player currentPlayer = game.GetCurrentPlayer();
            if (!playerGuid.Equals(currentPlayer.Id)) throw new InvalidOperationException($"Player {playerGuid} are not permitted for this action.");

            game.SellProperty(propertyGuid);
            
            await _eventPublisher.PublishGameActionEvent("PropertySold", gameGuid, new
            {
                PlayerId = playerGuid,
                PropertyGuid = propertyGuid,
                PlayerRemainingMoney = currentPlayer.Money
            });
        }

        public async Task UpgradeProperty(Guid gameGuid, Guid playerGuid, Guid propertyGuid)
        {
            GameState game = GetGame(gameGuid);
            Player currentPlayer = game.GetCurrentPlayer();

            if (!playerGuid.Equals(currentPlayer.Id)) throw new InvalidOperationException($"Player {playerGuid} are not permitted for this action.");

            game.UpgradeProperty(propertyGuid);

            await _eventPublisher.PublishGameActionEvent("PropertyUpgrade", gameGuid, new
            {
                PlayerId = playerGuid,
                PropertyGuid = propertyGuid,
                PlayerRemainingMoney = currentPlayer.Money
            });

        }
        public async Task DowngradeProperty(Guid gameGuid, Guid playerGuid, Guid propertyGuid)
        {
            GameState game = GetGame(gameGuid);
            Player currentPlayer = game.GetCurrentPlayer();

            if (!playerGuid.Equals(currentPlayer.Id)) throw new InvalidOperationException($"Player {playerGuid} are not permitted for this action.");

            game.DowngradeProperty(propertyGuid);

            await _eventPublisher.PublishGameActionEvent("PropertyDowngrade", gameGuid, new
            {
                PlayerId = playerGuid,
                PropertyGuid = propertyGuid,
                PlayerRemainingMoney = currentPlayer.Money
            });

        }
        public async Task MortgageProperty(Guid gameGuid, Guid playerGuid, Guid propertyGuid)
        {
            GameState game = GetGame(gameGuid);
            Player currentPlayer = game.GetCurrentPlayer();

            if (!playerGuid.Equals(currentPlayer.Id)) throw new InvalidOperationException($"Player {playerGuid} are not permitted for this action.");

            game.MortgageProperty(propertyGuid);

            await _eventPublisher.PublishGameActionEvent("PropertyMortgage", gameGuid, new
            {
                PlayerId = playerGuid,
                PropertyGuid = propertyGuid,
                PlayerRemainingMoney = currentPlayer.Money
            });

        }
        public async Task UnmortgageProperty(Guid gameGuid, Guid playerGuid, Guid propertyGuid)
        {
            GameState game = GetGame(gameGuid);
            Player currentPlayer = game.GetCurrentPlayer();

            if (!playerGuid.Equals(currentPlayer.Id)) throw new InvalidOperationException($"Player {playerGuid} are not permitted for this action.");

            game.UnmortgageProperty(propertyGuid);

            await _eventPublisher.PublishGameActionEvent("PropertyUnmortgage", gameGuid, new
            {
                PlayerId = playerGuid,
                PropertyGuid = propertyGuid,
                PlayerRemainingMoney = currentPlayer.Money
            });

        }

        public async Task EndTurn(Guid gameGuid, Guid playerGuid)
        {
            GameState game = GetGame(gameGuid);
            if (!playerGuid.Equals(game.GetCurrentPlayer().Id)) throw new InvalidOperationException($"Player {playerGuid} are not permitted for this action.");

            int nextPlayerIndex = game.EndTurn();

            await _eventPublisher.PublishGameActionEvent("EndTurn", gameGuid, new
            {
                NextPlayerIndex = nextPlayerIndex
            });
        }

        // Huge risk, use auth later to get the playerGuid
        public async Task DeclareBankcruptcy(Guid gameGuid, Guid playerGuid)
        {
            GameState game = GetGame(gameGuid);

            int nextPlayerIndex = game.DeclareBankcruptcy(playerGuid);

            await _eventPublisher.PublishGameActionEvent("DeclareBankcruptcy", gameGuid, new
            {
                RemovedPlayerGuid = playerGuid,
                NextPlayerIndex = nextPlayerIndex
            });

            if (game.ActivePlayers.Count == 1)
            {
                // TODO: GameControl win event
            }

        }

        // Dispose method for cleanup
        public void Dispose()
        {
            // If we need to dispose of any resources in the future
            // Currently, the IEventPublisher is responsible for disposing its own resources
        }
    }
