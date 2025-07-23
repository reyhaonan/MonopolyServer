using Microsoft.AspNetCore.SignalR;
using Confluent.Kafka;
using MonopolyServer.GameHubs;
using System.Text.Json;

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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
                                switch (eventType)
                                {
                                #region Game Control
                                    case "PlayerJoined":
                                        JsonElement playerJson = eventData.GetProperty("Players");
                                        var players = playerJson.Deserialize<List<Player>>() ?? throw new Exception("Player argument invalid");
                                        await _hubContext.Clients.Group(gameId.ToString()).JoinGameResponse(gameId, players);
                                    break;

                                    case "GameStart":
                                        var firstPlayerIndex = eventData.GetProperty("FirstPlayerIndex").GetInt32();

                                        await _hubContext.Clients.Group(gameId.ToString()).StartGameResponse(gameId, firstPlayerIndex);
                                        break;
                                // TODO: more event type handling

                                #endregion

                                #region Game Event
                                    case "DiceRolled":
                                        var roll1 = eventData.GetProperty("Roll1").GetInt32();
                                        var roll2 = eventData.GetProperty("Roll2").GetInt32();
                                        var totalRoll = eventData.GetProperty("TotalRoll").GetInt32();
                                        var newPosition = eventData.GetProperty("NewPosition").GetInt32();
                                        
                                        await _hubContext.Clients.Group(gameId.ToString()).DiceRolledResponse(gameId, roll1, roll2, totalRoll, newPosition);
                                    break;
                                    case "EndTurn":
                                        var nextPlayerIndex = eventData.GetProperty("NextPlayerIndex").GetInt32();
                                        
                                        await _hubContext.Clients.Group(gameId.ToString()).EndTurnResponse(gameId, nextPlayerIndex);
                                    break;
                                #endregion
                                default:
                                        _logger.LogWarning($"Unknown event type: {eventType}");
                                        break;
                                }
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