using System.Data.Common;
using Dapper;
using Org.BouncyCastle.Crypto;
using SHAW.DataAccess.Models;
using SHAW.DataAccess.Util;

namespace SHAW.DataAccess.Controllers;

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
                    + " WHERE username = @username AND password = @password;";
        object obj = new { username = login.Username, password = login.Password };
        string? loginKey = (await _connection.QueryAsync<string>(sql, obj))
            .ToList()
            .FirstOrDefault();

        return loginKey;    
    }
}