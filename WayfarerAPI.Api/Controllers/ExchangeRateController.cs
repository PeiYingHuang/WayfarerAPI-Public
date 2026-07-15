using Microsoft.AspNetCore.Mvc;
using WayfarerAPI.Application.Interfaces.Service;

namespace WayfarerAPI.Api.Controllers;

[Route("api/[controller]")]
public class ExchangeRateController : ApiControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;

    public ExchangeRateController(IExchangeRateService exchangeRateService)
    {
        _exchangeRateService = exchangeRateService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRate([FromQuery] string consumptionCurrencyCode, [FromQuery] string settlementCurrencyCode)
    {
        var result = await _exchangeRateService.GetRateAsync(consumptionCurrencyCode, settlementCurrencyCode);
        return ApiResult(result);
    }
}
