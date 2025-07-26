using Microsoft.AspNetCore.SignalR;
using Confluent.Kafka;
using MonopolyServer.GameHubs;
using System.Text.Json;
using MonopolyServer.Utils;

namespace MonopolyServer.Services
{
    public class KafkaSignalRNotifierService : BackgroundService
    {
        private readonly IHubContext<GameHubs.GameHubs, IResponse> _hubContext;
        private readonly ILogger<KafkaSignalRNotifierService> _logger;
        private IConsumer<string, string> _kafkaConsumer;

        private readonly IConfiguration Configuration;
        private GameService GameService;

        public KafkaSignalRNotifierService(IHubContext<GameHubs.GameHubs, IResponse> hubContext,
                                           ILogger<KafkaSignalRNotifierService> logger,GameService gameService, IConfiguration configuration)
        {
            _hubContext = hubContext;
            _logger = logger;

            GameService = gameService;

            Configuration = configuration;

            var consumerConfig = new ConsumerConfig
            {
                GroupId = Configuration["Kafka:ConsumerGroupId"], 
                BootstrapServers = Configuration["Kafka:BootstrapServers"],
                AutoOffsetReset = AutoOffsetReset.Earliest, 
                EnableAutoCommit = Configuration.GetValue<bool>("Kafka:EnableAutoCommit")
            };
            _kafkaConsumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        }

        #pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            _logger.LogInformation("Kafka SignalR Notifier Service starting.");

            var gameEventsTopic = Configuration["Kafka:GameEventsTopic"] ?? throw new Exception("Topic invalid");
            var gameControlTopic = Configuration["Kafka:GameControlTopic"] ?? throw new Exception("Topic invalid");

            _kafkaConsumer.Subscribe(new List<string> { gameEventsTopic, gameControlTopic });

            _ = Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _kafkaConsumer.Consume(stoppingToken);

                        var eventData = JsonSerializer.Deserialize<JsonElement>(consumeResult.Message.Value);

                        string eventType = eventData.GetProperty("EventType").GetString() ?? throw new Exception("Invalid Event");

                        _logger.LogInformation($"[{consumeResult.Topic}]: {eventType}");

                        Guid gameId = eventData.GetProperty("GameId").GetGuid();

                        try
                        {
                            #region Game Control
                            if (eventType == "PlayerJoined")
                            {
                                var players = eventData.GetProperty("Players").Deserialize<List<Player>>() ?? throw new Exception("Player argument invalid");
                                await _hubContext.Clients.Group(gameId.ToString()).JoinGameResponse(gameId, players);
                            }
                            else if (eventType == "GameStart")
                            {
                                var newPlayerOrder = eventData.GetProperty("NewPlayerOrder").Deserialize<List<Player>>() ?? throw new Exception("Invalid player list");
                                await _hubContext.Clients.Group(gameId.ToString()).StartGameResponse(gameId, newPlayerOrder);
                            }
                            #endregion

                            #region Game Event
                            // TODO: more event type handling
                            else if (eventType == "DiceRolled")
                            {
                                var result = eventData.GetProperty("RollResult").Deserialize<RollResult>();
                                var playerId = eventData.GetProperty("PlayerId").GetGuid();
                                await _hubContext.Clients.Group(gameId.ToString()).DiceRolledResponse(gameId, playerId, result);
                            }
                            else if (eventType == "EndTurn")
                            {
                                var nextPlayerIndex = eventData.GetProperty("NextPlayerIndex").GetInt32();
                                await _hubContext.Clients.Group(gameId.ToString()).EndTurnResponse(gameId, nextPlayerIndex);
                            }
                            #region Property event
                            else if (eventType == "PropertyBought")
                            {
                                var propertyGuid = eventData.GetProperty("PropertyGuid").GetGuid();
                                var playerRemainingMoney = eventData.GetProperty("PlayerRemainingMoney").GetDecimal();
                                var buyerId = eventData.GetProperty("PlayerId").GetGuid();
                                await _hubContext.Clients.Group(gameId.ToString()).PropertyBoughtResponse(gameId, buyerId, propertyGuid, playerRemainingMoney);
                            }
                            else if (eventType == "PropertySold")
                            {
                                var propertyGuid = eventData.GetProperty("PropertyGuid").GetGuid();
                                var playerRemainingMoney = eventData.GetProperty("PlayerRemainingMoney").GetDecimal();
                                var buyerId = eventData.GetProperty("PlayerId").GetGuid();
                                await _hubContext.Clients.Group(gameId.ToString()).PropertySoldResponse(gameId, buyerId, propertyGuid, playerRemainingMoney);
                            }
                            else if (eventType == "PropertyUpgrade")
                            {
                                var propertyGuid = eventData.GetProperty("PropertyGuid").GetGuid();
                                var playerRemainingMoney = eventData.GetProperty("PlayerRemainingMoney").GetDecimal();
                                var buyerId = eventData.GetProperty("PlayerId").GetGuid();
                                await _hubContext.Clients.Group(gameId.ToString()).PropertyUpgradeResponse(gameId, buyerId, propertyGuid, playerRemainingMoney);
                            }
                            else if (eventType == "PropertyDowngrade")
                            {
                                var propertyGuid = eventData.GetProperty("PropertyGuid").GetGuid();
                                var playerRemainingMoney = eventData.GetProperty("PlayerRemainingMoney").GetDecimal();
                                var buyerId = eventData.GetProperty("PlayerId").GetGuid();
                                await _hubContext.Clients.Group(gameId.ToString()).PropertyDowngradeResponse(gameId, buyerId, propertyGuid, playerRemainingMoney);
                            }
                            else if (eventType == "PropertyMortgage")
                            {
                                var propertyGuid = eventData.GetProperty("PropertyGuid").GetGuid();
                                var playerRemainingMoney = eventData.GetProperty("PlayerRemainingMoney").GetDecimal();
                                var buyerId = eventData.GetProperty("PlayerId").GetGuid();
                                await _hubContext.Clients.Group(gameId.ToString()).PropertyMortgagedResponse(gameId, buyerId, propertyGuid, playerRemainingMoney);
                            }
                            else if (eventType == "PropertyUnmortgage")
                            {
                                var propertyGuid = eventData.GetProperty("PropertyGuid").GetGuid();
                                var playerRemainingMoney = eventData.GetProperty("PlayerRemainingMoney").GetDecimal();
                                var buyerId = eventData.GetProperty("PlayerId").GetGuid();
                                await _hubContext.Clients.Group(gameId.ToString()).PropertyUnmortgagedResponse(gameId, buyerId, propertyGuid, playerRemainingMoney);
                            }
                            #endregion
                            #endregion
                            else
                            {
                                _logger.LogWarning($"Unknown event type: {eventType}");
                            }
                            _kafkaConsumer.Commit(consumeResult);
                            
                            
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing Kafka message.");
                        }
                        
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error consuming Kafka message.");
                        await Task.Delay(100, stoppingToken);
                    }
                }
                
                _logger.LogInformation("Kafka consumer task stopping.");
            }, stoppingToken);

            _logger.LogInformation("Kafka SignalR Notifier Service stopping.");
        }

        public override void Dispose()
        {
            _kafkaConsumer?.Close();
            _kafkaConsumer?.Dispose();
            base.Dispose();
        }
    }
}
