using SHAW.Util;
using MySql.Data.MySqlClient;
using Dapper;
using System.Text;

namespace SHAW.DataAccess.Util;

public class InvalidDatabaseVersionException : Exception {}

public static class DbConnectionFactory
{
    private static string DatabaseName = "shawdb";
    private static string InvalidVersionError = "Invalid version";
    private static int CurrentDatabaseVersion = 7;
    private static bool DatabaseLoaded = false;
    private static object LockDatabaseLoaded = new object();

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
        lock(LockDatabaseLoaded)
        {
            if(DatabaseLoaded)
            {
                return;
            }

            // Create database
            string connectionString = CreateMySqlConnectionString(username, password, "sys"); 
            using(AutoDbConnection conn = new AutoDbConnection(new MySqlConnection(connectionString))) 
            using(FileStream initFileStream = File.Open("./SqlFiles/initDatabase.sql", FileMode.Open))
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

            // Write current version to file
            using(FileStream versionFileStream = File.Open("./SqlFiles/database.version", FileMode.Create))
            {
                versionFileStream.Write(
                    Encoding.ASCII.GetBytes(CurrentDatabaseVersion.ToString())
                );
                
                versionFileStream.Close();
            }

            DatabaseLoaded = true;
        }
    }

    private static void DeleteExistingDatabase(IHostEnvironment env, string username, string password)
    {
        string connectionString = CreateMySqlConnectionString(username, password, "sys");
        using(AutoDbConnection conn = new AutoDbConnection(new MySqlConnection(connectionString)))
        {
            conn.Execute($"DROP DATABASE {DatabaseName};");
        }
    }

    private static AutoDbConnection GetCurrentVersionConnection(IHostEnvironment env, string username, string password)
    {
        string connectionString = CreateMySqlConnectionString(username, password);
        AutoDbConnection conn = new AutoDbConnection(new MySqlConnection(connectionString));

        try
        {
            int storedVersion;
            using(FileStream versionFileStream = File.Open("./SqlFiles/database.version", FileMode.Open))
            using(StreamReader reader = new StreamReader(versionFileStream))
            {
                string fileStr = reader.ReadToEnd();
                storedVersion = int.Parse(fileStr);
                versionFileStream.Close();
            }

            if(storedVersion != CurrentDatabaseVersion)
            {
                throw new InvalidDatabaseVersionException();
            }

            return conn;
        }
        catch(IOException e)
        {
            if(e.Message.Contains("being used"))
            {
                throw new Exception(e.Message);
            }

            throw new InvalidDatabaseVersionException();
        }
        catch(InvalidDatabaseVersionException)
        {
            DeleteExistingDatabase(env, username, password);
            throw new Exception(InvalidVersionError);
        }
    }

    private static AutoDbConnection TryCreateDbConnection(IHostEnvironment env, bool firstTime = true)
    {
        string? username = JsonHelper.GetJsonSecret("DatabaseUsername");
        string? password = JsonHelper.GetJsonSecret("DatabasePassword");

        if(username == null || password == null)
        {
            throw new Exception("Missing DatabaseConnectionString environment variable");
        }

        AutoDbConnection? connection;
        try 
        {
            connection = GetCurrentVersionConnection(env, username, password);
        } 
        catch(Exception e)
        {
            if(e.Message.Contains($"Unknown database '{DatabaseName}'") || e.Message.Equals(InvalidVersionError))
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
        return TryCreateDbConnection(env);
    }
}