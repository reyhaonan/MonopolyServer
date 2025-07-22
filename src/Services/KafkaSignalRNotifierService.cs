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

            _kafkaConsumer.Subscribe(Configuration["Kafka:GameEventsTopic"]);
            _kafkaConsumer.Subscribe(Configuration["Kafka:GameControlTopic"]);

            _ = Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _kafkaConsumer.Consume(stoppingToken);

                        _logger.LogInformation($"Consumed message from topic {consumeResult.Topic} at offset {consumeResult.Offset}: {consumeResult.Message.Value}");

                        var eventData = JsonSerializer.Deserialize<JsonElement>(consumeResult.Message.Value);
                        string eventType = eventData.GetProperty("EventType").GetString();
                        Guid gameId = eventData.GetProperty("GameId").GetGuid();

                        try
                            {
                                switch (eventType)
                                {
                                    #region Game Control
                                    case "PlayerJoined":
                                        JsonElement playerJson = eventData.GetProperty("Players");
                                        _logger.LogWarning(playerJson.ToString());
                                        var players = playerJson.Deserialize<List<Player>>() ?? throw new Exception("Player argument invalid");
                                        await _hubContext.Clients.Group(gameId.ToString()).JoinGameResponse(gameId, players);
                                    break;

                                    case "GameStart":
                                        await _hubContext.Clients.Group(gameId.ToString()).StartGameResponse(gameId);
                                        break;
                                    // TODO: more event type handling
                                        
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
            _kafkaConsumer?.Dispose();
            base.Dispose();
        }
    }
}