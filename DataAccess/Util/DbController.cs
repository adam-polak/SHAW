namespace SHAW.DataAccess.Util;

public abstract class DbController
{
    protected AutoDbConnection _connection;

    public DbController(AutoDbConnection connection)
    {
        _connection = connection;
    }
}