using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.Service;

namespace WayfarerAPI.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class TravelController : ApiControllerBase
{
    private readonly ITravelService _travelService;

    public TravelController(ITravelService travelService)
    {
        _travelService = travelService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var travellerId = GetCurrentUserId();
        var result = await _travelService.GetAllByTravellerAsync(travellerId);
        return ApiResult(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetAllSummary()
    {
        var travellerId = GetCurrentUserId();
        var result = await _travelService.GetAllSummaryByTravellerAsync(travellerId);
        return ApiResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var travellerId = GetCurrentUserId();
        var result = await _travelService.GetByIdAsync(id, travellerId);
        return ApiResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertTravelRequestDto request)
    {
        var travellerId = GetCurrentUserId();
        var result = await _travelService.CreateAsync(travellerId, request);
        return ApiResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertTravelRequestDto request)
    {
        var travellerId = GetCurrentUserId();
        var result = await _travelService.UpdateAsync(id, travellerId, request);
        return ApiResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var travellerId = GetCurrentUserId();
        await _travelService.DeleteAsync(id, travellerId);
        return ApiResult<bool>(true, "旅程已刪除", true);
    }
}
