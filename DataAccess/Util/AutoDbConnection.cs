using System.Data;
using System.Data.Common;

namespace SHAW.DataAccess.Util;

public class AutoDbConnection : DbConnection
{
    private DbConnection _connection;

    public AutoDbConnection(DbConnection connection)
    {
        _connection = connection;
        _connection.OpenAsync().Wait();
    }

    public override string ConnectionString 
    { 
        get => _connection.ConnectionString;
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
        set
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
        {
            _connection.ConnectionString = value;
        } 
    }

    public override string Database => _connection.Database;

    public override string DataSource => _connection.DataSource;

    public override string ServerVersion => _connection.ServerVersion;

    public override ConnectionState State => _connection.State;

    public override void ChangeDatabase(string databaseName)
    {
        _connection.ChangeDatabase(databaseName);
    }

    public override void Close()
    {
        throw new NotSupportedException();
    }

    public override ValueTask DisposeAsync()
    {
        _connection.CloseAsync().Wait();
        _connection.DisposeAsync().AsTask().Wait();
        return base.DisposeAsync();
    }

    public override void Open()
    {
        throw new NotSupportedException();
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        return _connection.BeginTransaction(isolationLevel);
    }

    protected override DbCommand CreateDbCommand()
    {
        return _connection.CreateCommand();
    }
}