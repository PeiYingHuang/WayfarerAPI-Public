using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayfarerAPI.Application.Interfaces.Services;

namespace WayfarerAPI.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class TravellerController : ApiControllerBase
{
    private readonly ITravellerService _travellerService;

    public TravellerController(ITravellerService travellerService)
    {
        _travellerService = travellerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetById(string email)
    {
        var travellar = await _travellerService.GetTravellerInfoByEmailAsync(email);
        return ApiResult(travellar, travellar == null ? "查無此帳號": "", true);
    }
}