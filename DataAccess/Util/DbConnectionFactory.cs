using System.Data.Common;
using SHAW.Util;
using MySql.Data.MySqlClient;

namespace SHAW.DataAccess.Util;

public static class DbConnectionFactory
{
    private static string CreateMySqlConnectionString(string username, string password)
    {
        return $"mysql://{username}:{password}@localhost:3306/shawdb";
    }

    private static DbConnection TryCreateDbConnection(IHostEnvironment env, bool firstTime)
    {
        string? username = JsonHelper.GetJsonSecret("DatabaseUsername");
        string? password = JsonHelper.GetJsonSecret("DatabasePassword");

        if(username == null || password == null)
        {
            throw new Exception("Missing DatabaseConnectionString environment variable");
        }

        DbConnection? connection;
        string connectionString = CreateMySqlConnectionString(username, password);
        try {
            connection = new MySqlConnection(connectionString);
        } catch(Exception e) {
            /*
             either server isn't started or "shawdb" hasn't been set up
             **TODO**

                 -determine which error appears for which case
                     - case server isn't started, throw a real exception stopping the program so that
                        the user can start the server

                     -case "shawdb" hasn't been setup, perform a setup then retry connection
            */
            Console.WriteLine(e);
            // IDEA: recursive call, with "firstTime" set to false so it won't recursively call anymore
            return TryCreateDbConnection(env, false);
        }
        
        return connection;
    }

    public static DbConnection CreateDbConnection(IHostEnvironment env)
    {
        return TryCreateDbConnection(env, true);
    }
}