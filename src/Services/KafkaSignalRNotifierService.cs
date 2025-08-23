using Microsoft.AspNetCore.SignalR;
using Confluent.Kafka;
using MonopolyServer.GameHubs;
using System.Text.Json;
using MonopolyServer.Utils;
using MonopolyServer.Models;

namespace MonopolyServer.Services;

public class KafkaSignalRNotifierService : BackgroundService
    {
        private readonly IHubContext<GameHubs.GameHubs, IResponse> _hubContext;
        private readonly ILogger<KafkaSignalRNotifierService> _logger;
        private IConsumer<string, string> _kafkaConsumer;

        private readonly IConfiguration Configuration;

        public KafkaSignalRNotifierService(IHubContext<GameHubs.GameHubs, IResponse> hubContext,
                                           ILogger<KafkaSignalRNotifierService> logger, IConfiguration configuration)
        {
            _hubContext = hubContext;
            _logger = logger;

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
                            else if (eventType == "UpdateGameConfig")
                            {
                                var newGameConfig = eventData.GetProperty("NewGameConfig").Deserialize<GameConfig>() ?? throw new Exception("Invalid game config");
                                await _hubContext.Clients.Group(gameId.ToString()).UpdateGameConfigResponse(gameId, newGameConfig);
                            }
                            else if (eventType == "GameOver")
                            {
                                await _hubContext.Clients.Group(gameId.ToString()).GameOverResponse(gameId);
                            }
                            #endregion

                            #region Game Event
                            else if (eventType == "DiceRolled")
                            {
                                var result = eventData.GetProperty("RollResult").Deserialize<RollResult>();
                                _logger.LogCritical($"TRANSACTION RESULT, {result.Transaction.Count}");
                                var playerId = eventData.GetProperty("PlayerId").GetGuid();
                                await _hubContext.Clients.Group(gameId.ToString()).DiceRolledResponse(gameId, playerId, result);
                            }
                            else if (eventType == "EndTurn")
                            {
                                var nextPlayerIndex = eventData.GetProperty("NextPlayerIndex").GetInt32();
                                await _hubContext.Clients.Group(gameId.ToString()).EndTurnResponse(gameId, nextPlayerIndex);
                            }
                            else if (eventType == "DeclareBankcruptcy")
                            {
                                var removedPlayerId = eventData.GetProperty("RemovedPlayerId").GetGuid();
                                var nextPlayerIndex = eventData.GetProperty("NextPlayerIndex").GetInt32();
                                await _hubContext.Clients.Group(gameId.ToString()).DeclareBankcruptcyResponse(gameId, removedPlayerId, nextPlayerIndex);
                            }
                            else if (eventType == "PayToGetOutOfJail")
                            {
                                var playerId = eventData.GetProperty("PlayerId").GetGuid();
                                var transactions = eventData.GetProperty("Transactions").Deserialize<List<TransactionInfo>>() ?? [];
                                await _hubContext.Clients.Group(gameId.ToString()).PayToGetOutOfJailResponse(gameId, playerId, transactions);
                            }
                            else if (eventType == "UseGetOutOfJailCard")
                            {
                                var playerId = eventData.GetProperty("PlayerId").GetGuid();
                                await _hubContext.Clients.Group(gameId.ToString()).UseGetOutOfJailCardResponse(gameId, playerId);
                            }
                            #region Property event
                            else if (eventType == "PropertyBought")
                            {
                                var propertyId = eventData.GetProperty("PropertyId").GetGuid();
                                var transactions = eventData.GetProperty("Transactions").Deserialize<List<TransactionInfo>>() ?? [];
                                var buyerId = eventData.GetProperty("PlayerId").GetGuid();
                                await _hubContext.Clients.Group(gameId.ToString()).PropertyBoughtResponse(gameId, buyerId, propertyId, transactions);
                            }
                            else if (eventType == "PropertySold")
                            {
                                var propertyId = eventData.GetProperty("PropertyId").GetGuid();
                                var transactions = eventData.GetProperty("Transactions").Deserialize<List<TransactionInfo>>() ?? [];
                                var buyerId = eventData.GetProperty("PlayerId").GetGuid();
                                await _hubContext.Clients.Group(gameId.ToString()).PropertySoldResponse(gameId, buyerId, propertyId, transactions);
                            }
                            else if (eventType == "PropertyUpgrade")
                            {
                                var propertyId = eventData.GetProperty("PropertyId").GetGuid();
                                var transactions = eventData.GetProperty("Transactions").Deserialize<List<TransactionInfo>>() ?? [];
                                await _hubContext.Clients.Group(gameId.ToString()).PropertyUpgradeResponse(gameId, propertyId, transactions);
                            }
                            else if (eventType == "PropertyDowngrade")
                            {
                                var propertyId = eventData.GetProperty("PropertyId").GetGuid();
                                var transactions = eventData.GetProperty("Transactions").Deserialize<List<TransactionInfo>>() ?? [];
                                await _hubContext.Clients.Group(gameId.ToString()).PropertyDowngradeResponse(gameId, propertyId, transactions);
                            }
                            else if (eventType == "PropertyMortgage")
                            {
                                var propertyId = eventData.GetProperty("PropertyId").GetGuid();
                                var transactions = eventData.GetProperty("Transactions").Deserialize<List<TransactionInfo>>() ?? [];
                                await _hubContext.Clients.Group(gameId.ToString()).PropertyMortgagedResponse(gameId, propertyId, transactions);
                            }
                            else if (eventType == "PropertyUnmortgage")
                            {
                                var propertyId = eventData.GetProperty("PropertyId").GetGuid();
                                var transactions = eventData.GetProperty("Transactions").Deserialize<List<TransactionInfo>>() ?? [];
                                await _hubContext.Clients.Group(gameId.ToString()).PropertyUnmortgagedResponse(gameId, propertyId, transactions);
                            }
                            #endregion
                            #region Trade event
                            else if (eventType == "InitiateTrade")
                            {
                                var trade = eventData.GetProperty("Trade").Deserialize<Trade>() ?? throw new Exception("Trade doesnt exist on InitiateTrade event");
                                await _hubContext.Clients.Group(gameId.ToString()).InitiateTradeResponse(gameId, trade);
                            }
                            else if (eventType == "AcceptTrade")
                            {
                                var trade = eventData.GetProperty("Trade").Deserialize<Trade>();
                                var transactions = eventData.GetProperty("Transactions").Deserialize<List<TransactionInfo>>() ?? [];
                                await _hubContext.Clients.Group(gameId.ToString()).AcceptTradeResponse(gameId, trade, transactions);
                            }
                            else if (eventType == "RejectTrade")
                            {
                                var tradeId = eventData.GetProperty("TradeId").GetGuid();
                                await _hubContext.Clients.Group(gameId.ToString()).RejectTradeResponse(gameId, tradeId);
                            }
                            else if (eventType == "CancelTrade")
                            {
                                var tradeId = eventData.GetProperty("TradeId").GetGuid();
                                await _hubContext.Clients.Group(gameId.ToString()).CancelTradeResponse(gameId, tradeId);
                            }
                            else if (eventType == "NegotiateTrade")
                            {
                                var trade = eventData.GetProperty("Trade").Deserialize<Trade>() ?? throw new Exception("Trade doesnt exist on NegotiateTrade event");
                                await _hubContext.Clients.Group(gameId.ToString()).NegotiateTradeResponse(gameId, trade);
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
