using SHAW.Util;
using MySql.Data.MySqlClient;

namespace SHAW.DataAccess.Util;

public static class DbConnectionFactory
{
    private static string DatabaseName = "shawdb";

    private static string CreateMySqlConnectionString(string username, string password)
    {
        return CreateMySqlConnectionString(username, password, DatabaseName);
    }

    private static string CreateMySqlConnectionString(string username, string password, string database)
    {
        return $"Server=localhost;Database={database};Uid={username};Pwd={password};";
    }

    private static void SetupDatabase(string username, string password)
    {
        string connectionString = CreateMySqlConnectionString(username, password, "sys"); 
        using(AutoDbConnection conn = new AutoDbConnection(new MySqlConnection(connectionString))) 
        {
            // TODO
        }
        throw new NotImplementedException();
    }

    private static AutoDbConnection TryCreateDbConnection(IHostEnvironment env, bool firstTime)
    {
        string? username = JsonHelper.GetJsonSecret("DatabaseUsername");
        string? password = JsonHelper.GetJsonSecret("DatabasePassword");

        if(username == null || password == null)
        {
            throw new Exception("Missing DatabaseConnectionString environment variable");
        }

        AutoDbConnection? connection;
        string connectionString = CreateMySqlConnectionString(username, password);
        try {
            connection = new AutoDbConnection(new MySqlConnection(connectionString));
        } catch(Exception e) {
            if(e.Message.Contains($"Unknown database '{DatabaseName}'"))
            {
                SetupDatabase(username, password);
            } else if(e.Message.Contains("Authentication")) {
                throw new Exception("Check README.md and ensure \"secrets.json\" matches expected format");
            }

            if(firstTime) 
            {
                return TryCreateDbConnection(env, false);
            }

            throw new Exception("Failed to create connection");
        }
        
        return connection;
    }

    public static AutoDbConnection CreateDbConnection(IHostEnvironment env)
    {
        return TryCreateDbConnection(env, true);
    }
}