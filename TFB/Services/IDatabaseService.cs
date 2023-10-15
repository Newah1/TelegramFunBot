using System.Data;
using System.Data.Common;

namespace TFB.Services;

public interface IDatabaseService
{
    IDbConnection Connect();
}