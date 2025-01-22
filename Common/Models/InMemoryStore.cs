using Common.Dtos;

namespace Common.Models
{
    public class InMemoryStore
    {
        // A simple list to store messages in memory.
        public List<MessageDto> Messages { get; } = new List<MessageDto>();

        public void Add(MessageDto message)
        {
            Messages.Add(message);
        }
    }
}
