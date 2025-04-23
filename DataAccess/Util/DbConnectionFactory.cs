using SHAW.Util;
using MySql.Data.MySqlClient;
using Dapper;
using System.Text;

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

    private static string? GetNextSqlCommand(StreamReader reader)
    {
        string? line;
        bool isComment = false;
        StringBuilder sb = new StringBuilder();
        while((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if(line.Length == 0) {
                continue;
            } else if(!isComment) {
                if(line.StartsWith("/*"))
                {
                    isComment = !line.EndsWith("*/");
                    continue;
                } else if(line.StartsWith('#'))
                {
                    continue;
                }
            } else {
                isComment = !line.EndsWith("*/");
                continue;
            }

            sb.Append(line);
            if(line.EndsWith(';'))
            {
                break;
            }
        }

        if(line == null || sb.Length == 0)
        {
            return null;
        }

        return sb.ToString();
    }

    private static void SetupDatabase(string username, string password)
    {
        string connectionString = CreateMySqlConnectionString(username, password, "sys"); 
        using(AutoDbConnection conn = new AutoDbConnection(new MySqlConnection(connectionString))) 
        {
            FileStream initFileStream = File.Open("./SqlFiles/initDatabase.sql", FileMode.Open);
            using(StreamReader reader = new StreamReader(initFileStream))
            {
                string? sqlCommand;
                while((sqlCommand = GetNextSqlCommand(reader)) != null)
                {
                    conn.Execute(sqlCommand);
                    if(sqlCommand.StartsWith("CREATE DATABASE"))
                    {
                        conn.ChangeDatabase(DatabaseName);
                    }
                }
            }
        }
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