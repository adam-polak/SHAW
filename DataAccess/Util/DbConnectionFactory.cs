using System.Data.Common;
using SHAW.Util;
using MySql.Data.MySqlClient;

namespace SHAW.DataAccess.Util;

public static class DbConnectionFactory
{
    public static DbConnection CreateDbConnection(IHostEnvironment env)
    {
        string? connectionString;
        if(env.IsDevelopment())
        {
            connectionString = JsonHelper.GetJsonSecret("DatabaseConnectionString");
        } else {
            connectionString = Environment.GetEnvironmentVariable("DatabaseConnectionString");
        }

        if(connectionString == null)
        {
            throw new Exception("Missing DatabaseConnectionString environment variable");
        }

        return new MySqlConnection(connectionString);
    }
}