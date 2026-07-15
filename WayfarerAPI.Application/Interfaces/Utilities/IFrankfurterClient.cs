using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Application.Interfaces.Utilities;

public interface IFrankfurterClient
{
    Task<List<ConfigCurrency>> GetCurrenciesAsync();
}