using System.Runtime.InteropServices;
using Dapper;
using Microsoft.Extensions.Logging;
using TFB.Models;

namespace TFB.Services;

public class MessageHistoryService : IMessageHistoryService
{
    private readonly ILogger<IMessageHistoryService> _logger;
    private readonly IDatabaseService _databaseService;

    private const string _tableName = "MessageHistories";

    public MessageHistoryService(ILogger<IMessageHistoryService> logger, IDatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    public async Task<MessageHistory?> UpdateMessageSummary(MessageHistory messageHistory)
    {
        var query = $"UPDATE {_tableName} SET Summary=@Summary WHERE MessageHistoryId=@MessageHistoryId;" +
                    $"SELECT * FROM {_tableName} WHERE MessageHistoryId=@MessageHistoryId;";

        using var connection = _databaseService.Connect();

        var message = await connection.QueryFirstOrDefaultAsync<MessageHistory>(query, messageHistory);

        return message;
    }
    
    public async Task<MessageHistory?> AddMessage(MessageHistory messageHistory)
    {
        var query =
            $"INSERT INTO {_tableName} (Author, Role, DateCreated, Message, PersonalityId) VALUES (@Author, @Role, @DateCreated, @Message, @PersonalityId);" +
            $"SELECT * FROM {_tableName} WHERE MessageHistoryId=last_insert_rowid();";


        using var connection = _databaseService.Connect();

        var insertedMessage = await connection.QueryFirstOrDefaultAsync<MessageHistory>(query, messageHistory);

        return insertedMessage;
    }

    public async Task<int> WipeMessagesByPersonality(int personalityId)
    {
        var query = $"DELETE FROM {_tableName} WHERE PersonalityId=@personalityId";

        using var connection = _databaseService.Connect();

        var executed = await connection.ExecuteAsync(query, new {personalityId = personalityId});
        
        _logger.LogDebug($"Deleted {executed} messages.");

        return executed;
    }

    public async Task<IEnumerable<MessageHistory>> GetAlLMessagesByAuthor(string author)
    {
        var query = $"SELECT * FROM {_tableName} WHERE Author=@Author";

        using var connection = _databaseService.Connect();

        var messages = await connection.QueryAsync<MessageHistory>(query);
        
        return messages;
    }
}