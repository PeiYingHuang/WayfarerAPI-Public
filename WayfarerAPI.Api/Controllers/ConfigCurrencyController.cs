using Microsoft.AspNetCore.Mvc;
using WayfarerAPI.Application.Interfaces.Service;

namespace WayfarerAPI.Api.Controllers;

[Route("api/[controller]")]
public class ConfigCurrencyController : ApiControllerBase
{
    private readonly IConfigCurrencyService _configCurrencyService;

    public ConfigCurrencyController(IConfigCurrencyService configCurrencyService)
    {
        _configCurrencyService = configCurrencyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _configCurrencyService.GetAllAsync();
        return ApiResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Sync()
    {
        var result = await _configCurrencyService.SyncAsync();
        return ApiResult(result);
    }
}