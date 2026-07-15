using System.Data;

namespace WayfarerAPI.Application.Interfaces.Data;

public interface IWayfarerDbConnectionFactory
{
    IDbConnection CreateConnection();
}
