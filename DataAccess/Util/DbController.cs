using Dapper;

namespace SHAW.DataAccess.Util;

public abstract class DbController
{
    private AutoDbConnection _connection;

    public DbController(AutoDbConnection connection)
    {
        _connection = connection;
    }

    protected void DoCommand(string sql, object[] parameters)
    {
        _connection.Execute(sql, parameters);
    }

    protected async Task DoCommandAsync(string sql, object[] parameters)
    {
        await _connection.ExecuteAsync(sql, parameters);
    }

    protected async Task DoCommandAsync(string sql)
    {
        await _connection.ExecuteAsync(sql);
    }

    protected async Task DoCommandAsync(string sql, object parameters)
    {
        await _connection.ExecuteAsync(sql, parameters);
    }

    protected List<T> DoQuery<T>(string sql, object parameters)
    {
        List<T> list = _connection.Query<T>(sql, parameters).AsList();

        return list;
    }

    protected List<T> DoQuery<T>(string sql)
    {
        List<T> list = _connection.Query<T>(sql).AsList();
        
        return list;
    }

    protected async Task<List<T>> DoQueryAsync<T>(string sql)
    {
        List<T> list = (await _connection.QueryAsync<T>(sql)).AsList();

        return list;
    }

    protected async Task<List<T>> DoQueryAsync<T>(string sql, object parameters)
    {
        List<T> list = (await _connection.QueryAsync<T>(sql, parameters)).AsList();

        return list;
    }
}