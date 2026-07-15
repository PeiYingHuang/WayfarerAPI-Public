namespace WayfarerAPI.Application.Interfaces.Data;

public interface IUnitOfWork
{
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
