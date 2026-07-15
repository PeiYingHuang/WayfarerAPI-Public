using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Application.Interfaces.Repositories;

public interface IConfigCurrencyRepository
{
    Task<bool> ExistsByCodeAsync(string code);
    Task<IEnumerable<ConfigCurrency>> GetAllAsync();
    Task UpsertAsync(List<ConfigCurrency> currencies);
}
