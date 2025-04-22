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

    public override string ConnectionString { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override string Database => throw new NotImplementedException();

    public override string DataSource => throw new NotImplementedException();

    public override string ServerVersion => throw new NotImplementedException();

    public override ConnectionState State => throw new NotImplementedException();

    public override void ChangeDatabase(string databaseName)
    {
        throw new NotImplementedException();
    }

    public override void Close()
    {
        throw new NotImplementedException();
    }

    public override ValueTask DisposeAsync()
    {
        _connection.CloseAsync().Wait();
        _connection.DisposeAsync();
        return base.DisposeAsync();
    }

    public override void Open()
    {
        throw new NotImplementedException();
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        throw new NotImplementedException();
    }

    protected override DbCommand CreateDbCommand()
    {
        throw new NotImplementedException();
    }
}