using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.Service;
using WayfarerAPI.Application.Interfaces.Utilities;

namespace WayfarerAPI.Application.Services;

public sealed class ExchangeRateService : IExchangeRateService
{
    private readonly IExchangeRateClient _exchangeRateClient;

    public ExchangeRateService(IExchangeRateClient exchangeRateClient)
    {
        _exchangeRateClient = exchangeRateClient;
    }

    public async Task<ExchangeRateDto> GetRateAsync(string consumptionCurrencyCode, string settlementCurrencyCode)
    {
        if (string.IsNullOrWhiteSpace(consumptionCurrencyCode) || string.IsNullOrWhiteSpace(settlementCurrencyCode))
            throw new ArgumentException("ConsumptionCurrencyCode 與 SettlementCurrencyCode 都是必填");

        var baseCode = consumptionCurrencyCode.Trim().ToUpperInvariant();
        var targetCode = settlementCurrencyCode.Trim().ToUpperInvariant();

        var rate = await _exchangeRateClient.GetRateAsync(baseCode, targetCode);

        return new ExchangeRateDto
        {
            ConsumptionCurrencyCode = baseCode,
            SettlementCurrencyCode = targetCode,
            Rate = rate
        };
    }
}
