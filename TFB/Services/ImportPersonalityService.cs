using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TFB.DTOs;
using TFB.Models;
using Personality = TFB.DTOs.Personality;

namespace TFB.Services;

public class ImportPersonalityService : IImportPersonalityService
{
    private readonly ILogger<IImportPersonalityService> _logger;
    private readonly PersonalitySheetsService _personalitySheetsService;
    private readonly IConfiguration _configuration;
    private readonly IDatabaseService _databaseService;
    private const string _tableName = "Personalities";
    
    public IEnumerable<Personality> Personalities { get; set; } 

    public ImportPersonalityService(ILogger<IImportPersonalityService> logger, PersonalitySheetsService personalitySheetsService, IConfiguration configuration, IDatabaseService databaseService)
    {
        _logger = logger;
        _personalitySheetsService = personalitySheetsService;
        _configuration = configuration;
        _databaseService = databaseService;
    }
    
    public async Task<IEnumerable<Personality>> ImportPersonalities()
    {
        var personalitiesFromSheet = _personalitySheetsService.LoadPersonalities();
        
        var personalitiesFromSettings = new List<Personality>();
        _configuration.GetSection("Personalities").Bind(personalitiesFromSettings);

        List<Personality> personalities = new List<Personality>();

        personalities.AddRange(personalitiesFromSettings);
        personalities.AddRange(personalitiesFromSheet);

        var query =
            $"INSERT INTO Personalities (Name, PersonalityDescription, Command, Temperature, HasOptions, Model) VALUES (@Name, @PersonalityDescription, @Command, @Temperature, @HasOptions, @Model)" +
            $"ON CONFLICT(Command) DO UPDATE SET PersonalityDescription=excluded.PersonalityDescription, Temperature=excluded.Temperature, Model=excluded.Model";

        using var connection = _databaseService.Connect();

        await connection.ExecuteAsync(query, personalities);

        Personalities = personalities;
        
        _logger.LogDebug($"Loaded {personalities.Count} personalities.");

        return personalities;
    }
}