using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace TFB.Services;

public class DatabaseService : IDatabaseService
{
    private readonly IConfiguration _configuration;
    
    public DatabaseService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public IDbConnection Connect()
    {
        return new SqliteConnection(_configuration.GetConnectionString("tfb"));
    }
}