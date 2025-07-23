// MonopolyServer/Services/GameService.cs
using Microsoft.AspNetCore.SignalR;
using MonopolyServer.GameHubs;
using Confluent.Kafka; // Kafka Producer
using System;
using System.Collections.Concurrent;
using System.Text.Json; // For serialization to JSON
using System.Threading.Tasks;

namespace MonopolyServer.Services
{
    public class GameService
    {
        private static readonly ConcurrentDictionary<Guid, GameState> _activeGames = new();

        // Kafka Producer for game events
        private readonly IProducer<string, string> _kafkaProducer; // Key: GameId, Value: JSON serialized event

        private readonly IConfiguration Configuration;

        public GameService(IConfiguration configuration)
        {
            Configuration = configuration;

            // --- Configure Kafka Producer ---
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = Configuration["Kafka:BootstrapServers"],
            };
            _kafkaProducer = new ProducerBuilder<string, string>(producerConfig).Build();
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

            var gameCreatedEvent = new { EventType = "GameCreated", Game = newGame };
            await _kafkaProducer.ProduceAsync(Configuration["Kafka:GameControlTopic"], new Message<string, string>
            {
                Key = gameGuid.ToString(),
                Value = JsonSerializer.Serialize(gameCreatedEvent)
            });

            return (gameGuid, newGame);
        }
        

        public async Task<Player> AddPlayerToGame(Guid gameGuid, string playerName)
        {
            GameState game = GetGame(gameGuid);
            if (game == null) throw new InvalidOperationException($"Game {gameGuid} not found.");

            Player newPlayer = new Player(playerName);
            game.AddPlayer(newPlayer);

            var playerJoinedEvent = new { EventType = "PlayerJoined", GameId = gameGuid, Players = game.ActivePlayers };
            await _kafkaProducer.ProduceAsync(Configuration["Kafka:GameControlTopic"], new Message<string, string>
            {
                Key = gameGuid.ToString(),
                Value = JsonSerializer.Serialize(playerJoinedEvent)
            });

            return newPlayer;
        }

        public async Task StartGame(Guid gameGuid)
        {
            GameState game = GetGame(gameGuid);
            game.StartGame();

            var gameStartEvent = new { EventType = "GameStart", GameId = gameGuid, FirstPlayerIndex = game.CurrentPlayerIndex };
            await _kafkaProducer.ProduceAsync(Configuration["Kafka:GameControlTopic"], new Message<string, string>
            {
                Key = gameGuid.ToString(),
                Value = JsonSerializer.Serialize(gameStartEvent)
            });
        }

        public async Task ProcessDiceRoll(Guid gameGuid, Guid playerGuid)
        {
            GameState game = GetGame(gameGuid);
            if (game == null) throw new InvalidOperationException($"Game {gameGuid} not found.");
            if(!playerGuid.Equals(game.GetCurrentPlayer().Id))throw new InvalidOperationException($"Player {playerGuid} are not permitted for this action.");

            (int roll1, int roll2, int totalRoll, bool wasJailed, Player? jailedPlayer, int newPlayerPosition) = game.RollDice();
            
            var diceRolledEvent = new {
                EventType = "DiceRolled",
                GameId = gameGuid,
                PlayerId = playerGuid,
                Roll1 = roll1,
                Roll2 = roll2,
                TotalRoll = totalRoll,
                NewPosition = newPlayerPosition};
            await _kafkaProducer.ProduceAsync(Configuration["Kafka:GameEventsTopic"], new Message<string, string>
            {
                Key = gameGuid.ToString(),
                Value = JsonSerializer.Serialize(diceRolledEvent)
            });

            if (wasJailed && jailedPlayer != null)
            {
                var playerJailedEvent = new { EventType = "PlayerJailed", GameId = gameGuid, PlayerId = jailedPlayer.Id, TurnsInJail = 3 };
                await _kafkaProducer.ProduceAsync(Configuration["Kafka:GameEventsTopic"], new Message<string, string>
                {
                    Key = gameGuid.ToString(),
                    Value = JsonSerializer.Serialize(playerJailedEvent)
                });
            }
        }

        public async Task EndTurn(Guid gameGuid, Guid playerGuid)
        {
            GameState game = GetGame(gameGuid);
            if (game == null) throw new InvalidOperationException($"Game {gameGuid} not found.");
            if (!playerGuid.Equals(game.GetCurrentPlayer().Id)) throw new InvalidOperationException($"Player {playerGuid} are not permitted for this action.");

            int nextPlayerIndex = game.EndTurn();

            var endTurnEvent = new
            {
                EventType = "EndTurn",
                GameId = gameGuid,
                NextPlayerIndex = nextPlayerIndex,
            };

            await _kafkaProducer.ProduceAsync(Configuration["Kafka:GameEventsTopic"], new Message<string, string>
            {
                Key = gameGuid.ToString(),
                Value = JsonSerializer.Serialize(endTurnEvent)
            });
        }

        // Dispose the producer when the app shuts down
        public void Dispose()
        {
            _kafkaProducer.Flush(TimeSpan.FromSeconds(10));
            _kafkaProducer.Dispose();
        }
    }
}