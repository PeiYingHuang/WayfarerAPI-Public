namespace WayfarerAPI.Application.Interfaces.Utilities;

public interface IExchangeRateClient
{
    Task<decimal> GetRateAsync(string baseCurrencyCode, string targetCurrencyCode);
}
