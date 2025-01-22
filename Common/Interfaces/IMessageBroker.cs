using Common.Dtos;

namespace Common.Interfaces
{
    public interface IMessageBroker
    {
        Task Publish(MessageDto message);
        Task Subscribe(Action<MessageDto> onMessageReceived);
    }
}
