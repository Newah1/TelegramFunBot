using Dapper;
using Microsoft.Extensions.Logging;
using TFB.Models;

namespace TFB.Services;

public class PersonService : IPersonService
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<IPersonService> _logger;
    private const string _tableName = "People";

    public PersonService(IDatabaseService databaseService, ILogger<IPersonService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task<IEnumerable<Person>> GetAllPeople()
    {
        var query = $"SELECT * FROM {_tableName}";

        using var connection = _databaseService.Connect();

        var people = await connection.QueryAsync<Person>(query);

        return people;
    }

    public async Task<Person?> GetPersonByUsername(string username)
    {
        var query = $"SELECT * FROM {_tableName} WHERE Username=@username";

        using var connection = _databaseService.Connect();

        var person = await connection.QueryFirstOrDefaultAsync<Person>(query, new { username = username });

        return person;
    }

    public async Task<Person?> CreateOrUpdate(Person person)
    {
        var query =
            $"INSERT INTO People (Username, FirstSeen, LastSeen) VALUES (@Username, @FirstSeen, @LastSeen)" +
            $"ON CONFLICT (Username) DO UPDATE SET LastSeen=@LastSeen; SELECT * FROM {_tableName} WHERE Username=@Username";

        using var connection = _databaseService.Connect();

        var insertedPerson = await connection.QueryFirstOrDefaultAsync<Person>(query, person);

        return insertedPerson;
    }
}