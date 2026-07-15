using MySqlConnector;
using System.Data;
using WayfarerAPI.Application.Interfaces.Data;

namespace WayfarerAPI.Infrastructure.Data;

public class WayfarerDbConnectionFactory : IWayfarerDbConnectionFactory
{
    private readonly string _connectionString;

    public WayfarerDbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        return new MySqlConnection(_connectionString);
    }
}
