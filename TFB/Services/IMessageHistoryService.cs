using TFB.Models;

namespace TFB.Services;

public interface IMessageHistoryService
{
    Task<MessageHistory> AddMessage(MessageHistory messageHistory);
    Task<MessageHistory?> UpdateMessageSummary(MessageHistory messageHistory);
    Task<int> WipeMessagesByPersonality(int personalityId);
    Task<IEnumerable<MessageHistory>> GetAlLMessagesByAuthor(string author);
}