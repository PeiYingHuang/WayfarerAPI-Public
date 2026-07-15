using System.Data;
using WayfarerAPI.Application.Interfaces.Data;

namespace WayfarerAPI.Infrastructure.Data;

public sealed class DbSession : IDbSession, IUnitOfWork, IDisposable
{
    private readonly IWayfarerDbConnectionFactory _factory;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;

    public DbSession(IWayfarerDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public IDbConnection Connection => _connection ??= _factory.CreateConnection();

    public IDbTransaction? Transaction => _transaction;

    public Task BeginTransactionAsync()
    {
        _connection ??= _factory.CreateConnection();

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        _transaction = _connection.BeginTransaction();
        return Task.CompletedTask;
    }

    public Task CommitAsync()
    {
        _transaction?.Commit();
        _transaction?.Dispose();
        _transaction = null;
        return Task.CompletedTask;
    }

    public Task RollbackAsync()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _transaction = null;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _connection?.Dispose();
    }
}
