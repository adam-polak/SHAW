using System.Data.Common;
using System.Security.Cryptography;
using Dapper;
using SHAW.DataAccess.Models;
using SHAW.DataAccess.Util;

namespace SHAW.DataAccess.Controllers;

public class UsernameExistsException : Exception
{
    // Empty
}

public class UserController : AutoDbConnection
{
    public UserController(DbConnection connection) : base(connection) {}

    /// <summary>
    /// Check if the login is correct. If it is this will return a login key to use
    /// for the user.
    /// </summary>
    /// <param name="login"></param>
    /// <returns>A login key if the login is correct or null</returns>
    public async Task<string?> CorrectLogin(LoginModel login)
    {
        string sql = "SELECT loginkey FROM users"
                    + " WHERE username = @Username AND password = @Password;";
        string? loginKey;
        try 
        {
            loginKey = (await _connection.QueryAsync<string>(sql, login))
                .ToList()
                .First()
                ?? ""; 
        }
        catch
        {
            return null;
        }

        if(loginKey.Length == 0)
        {
            // Generate a login key
            loginKey = LoginKey.Create();
            sql = "UPDATE users SET loginkey = @Key"
                + " WHERE username = @Username AND password = @Password";
            object obj = new { Key = loginKey, login.Username, login.Password };
            await _connection.ExecuteAsync(sql, obj);
        }

        return loginKey;    
    }

    /// <summary>
    /// Try to create a user with the given Username and Password
    /// </summary>
    /// <param name="user">The LoginModel to create a user based off</param>
    /// <returns></returns>
    public async Task CreateUserOrThrow(CreateUserModel user)
    {
        string sql = "INSERT INTO users (username, password, roleid)"
                    + " VALUES (@Username, @Password, @Role);";
        try
        {
            await _connection.ExecuteAsync(sql, user);
        }
        catch
        {
            throw new UsernameExistsException();
        }
    }
    
    /// <summary>
    /// Try to validate a login-key for a user
    /// </summary>
    /// <param name="loginKey">The LoginKey provided when you log in</param>
    public async Task<bool?> ValidateLoginKey(string key)
    {
        int count;
        string sql = "SELECT COUNT(*) FROM users WHERE LoginKey = @Key";
        try
        { 
            count = (await _connection
                    .QueryAsync<int>(sql, new {Key = key}))
                    .First();
        }
        catch (Exception ex)
        {
            return null;
        }
        return count > 0;
    }


}

public static class LoginKey
{
    public static string Create()
    {
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            byte[] bytes = new byte[32];
            rng.GetBytes(bytes);
            return Base64UrlEncode(bytes);
        }
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
                      .TrimEnd('=')
                      .Replace('+', '-')
                      .Replace('/', '_');
    }
}