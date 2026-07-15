using System.Data;

namespace WayfarerAPI.Application.Interfaces.Data;

public interface IDbSession
{
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }
}
