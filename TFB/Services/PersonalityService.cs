using Dapper;
using Google.Apis.Logging;
using Microsoft.Extensions.Logging;
using TFB.Models;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace TFB.Services;

public class PersonalityService : IPersonalityService
{
    private List<Personality> _personalities;

    private readonly IImportPersonalityService _importPersonalityService;
    private const string _tableName = "Personalities";
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<IPersonalityService> _logger;

    private DateTime _lastCheck;
    private DateTime _nextExpiration;
    private TimeSpan _relativeCheckTime;
    
    public PersonalityService(IImportPersonalityService importPersonalityService, IDatabaseService databaseService, ILogger<IPersonalityService> logger)
    {
        _importPersonalityService = importPersonalityService;
        _personalities = new List<Personality>();
        _databaseService = databaseService;
        _logger = logger;

        _relativeCheckTime = TimeSpan.FromMinutes(2);
        
        ResetExpiration();
    }

    private void ResetExpiration()
    {
        _nextExpiration = DateTime.Now + _relativeCheckTime;
    }
    
    private async Task ImportPersonalities()
    {
        await _importPersonalityService.ImportPersonalities();
    }

    public IEnumerable<Personality> GetPersonalities()
    {
        return _personalities;
    }

    public async Task<int?> UpdatePersonality(int personalityId, string name, string personalityDescription)
    {
        var query = @"
        UPDATE Personalities
        SET PersonalityDescription = @Description, Name = @Name
        WHERE PersonalityId = @PersonalityId;

        SELECT PersonalityId FROM Personalities
        WHERE Name = @Name AND PersonalityDescription = @Description;
    ";

        using var connection = _databaseService.Connect();

        var insertedId = await connection.QueryFirstOrDefaultAsync<int>(query, new { Name = name, Description = personalityDescription, PersonalityId = personalityId });

        return insertedId;
    }
    public async Task<IEnumerable<Models.Personality>> GetPersonalities(PersonalityRequest request)
    {
        if (DateTime.Now >= _nextExpiration)
        {
            ResetExpiration();
            await ImportPersonalities();
        }
        
        string query = $"SELECT * FROM {_tableName} p ";

        if (request.IncludeMessageHistory)
        {
            query += " LEFT JOIN MessageHistories mh ON mh.PersonalityId=p.PersonalityId";
        }
        
        if (request.PersonalityId != null)
        {
            query += " WHERE p.PersonalityId=@PersonalityId";
        }
        
        

        if (request.IncludeMessageHistory)
        {
            query += " ORDER BY mh.DateCreated ASC";
        }
        
        if (request.Limit != null)
        {
            query += $" LIMIT {request.Limit}";
        }

        if (request.Offset != null)
        {
            query += $" OFFSET {request.Offset}";
        }

        object param = new { };

        if (request.PersonalityId != null)
        {
            param = new { request.PersonalityId };
        }
        
        using var connection = _databaseService.Connect();

        var personalities = new Dictionary<int, Personality>();
        
        await connection.QueryAsync<Personality, MessageHistory, Personality>(query,
            (personality, messageHistory) =>
            {
                if (!personalities.ContainsKey(personality.PersonalityId))
                {
                    personalities.Add(personality.PersonalityId, personality);
                }

                var currentPersonality = personalities[personality.PersonalityId];

                if (messageHistory != null)
                {
                    currentPersonality.MessageHistories.Add(messageHistory);
                }

                return personality;
            }, splitOn: "MessageHistoryId", param: param);

        return personalities.Values;
    }

}