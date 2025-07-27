using System.Text.Json;
using Confluent.Kafka;

namespace MonopolyServer.Services;

public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _kafkaProducer;
    private readonly IConfiguration _configuration;

    public KafkaEventPublisher(IConfiguration configuration)
    {
        _configuration = configuration;
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
        };
        _kafkaProducer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public async Task PublishGameControlEvent(string eventType, Guid gameId, object eventData)
    {
        var fullEvent = new Dictionary<string, object>
        {
            { "EventType", eventType },
            { "GameId", gameId }
        };
        
        foreach (var prop in eventData.GetType().GetProperties())
        {
            fullEvent[prop.Name] = prop.GetValue(eventData);
        }
        
        await _kafkaProducer.ProduceAsync(_configuration["Kafka:GameControlTopic"], new Message<string, string>
        {
            Key = gameId.ToString(),
            Value = JsonSerializer.Serialize(fullEvent)
        });
    }

    public async Task PublishGameActionEvent(string eventType, Guid gameId, object eventData)
    {
        var fullEvent = new Dictionary<string, object>
        {
            { "EventType", eventType },
            { "GameId", gameId }
        };
        
        foreach (var prop in eventData.GetType().GetProperties())
        {
            fullEvent[prop.Name] = prop.GetValue(eventData);
        }
        
        await _kafkaProducer.ProduceAsync(_configuration["Kafka:GameEventsTopic"], new Message<string, string>
        {
            Key = gameId.ToString(),
            Value = JsonSerializer.Serialize(fullEvent)
        });
    }

    public void Dispose()
    {
        _kafkaProducer?.Flush(TimeSpan.FromSeconds(10));
        _kafkaProducer?.Dispose();
    }
}
