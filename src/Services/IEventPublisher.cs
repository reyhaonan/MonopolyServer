using System;
using System.Threading.Tasks;

namespace MonopolyServer.Services
{
    public interface IEventPublisher
    {
        Task PublishGameControlEvent(string eventType, Guid gameId, object eventData);
        Task PublishGameActionEvent(string eventType, Guid gameId, object eventData);
    }
}