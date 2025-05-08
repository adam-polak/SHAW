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
    /// Try to get the user from with the provided login key. Throws an error if the
    /// login key is invalid.
    /// </summary>
    /// <param name="loginKey"></param>
    /// <returns>User associated with the login key</returns>
    public async Task<UserModel> TryGetUser(string loginKey)
    {
        string sql = "SELECT id, username, roleid FROM users"
                    + " WHERE loginkey = @Key";
        object obj = new { Key = loginKey };
        return (await _connection.QueryAsync<UserModel>(sql, obj)).First();
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
        catch
        {
            return null;
        }
        return count > 0;
    }

    public async Task<RoleType?> GetUserRole(string loginKey)
    {
        string sql = "SELECT roleid FROM users WHERE loginkey = @Key";
        try
        {
            return await _connection.QueryFirstOrDefaultAsync<RoleType>(sql, new { Key = loginKey });
        }
        catch
        {
            return null;
        }
    }

    // We can also complete the IsCounselor method that was started:
    public async Task<bool> IsCounselor(string loginKey)
    {
        var role = await GetUserRole(loginKey);
        return role == RoleType.Counselor;
    }

    public async Task<int?> GetUserIdFromLoginKey(string loginKey)
    {
        string sql = "SELECT id FROM users WHERE loginkey = @LoginKey";
        
        try
        {
            return await _connection.QueryFirstOrDefaultAsync<int?>(sql, new
            {
                LoginKey = loginKey
            });
        }
        catch
        {
            return null;
        }
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