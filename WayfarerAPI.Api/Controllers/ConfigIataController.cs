using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayfarerAPI.Application.Interfaces.Service;

namespace WayfarerAPI.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class ConfigIataController : ApiControllerBase
{
    private readonly IConfigIataService _configIataService;

    public ConfigIataController(IConfigIataService configIataService)
    {
        _configIataService = configIataService;
    }

    [HttpGet("{iataCode}")]
    public async Task<IActionResult> GetByIataCode(string iataCode)
    {
        var result = await _configIataService.GetAirportByIataCodeAsync(iataCode);
        return ApiResult(result);
    }
}
