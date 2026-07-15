using WayfarerAPI.Application.DTOs;

namespace WayfarerAPI.Application.Interfaces.Service;

public interface IExchangeRateService
{
    Task<ExchangeRateDto> GetRateAsync(string consumptionCurrencyCode, string settlementCurrencyCode);
}
