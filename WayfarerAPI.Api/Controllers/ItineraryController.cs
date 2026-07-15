using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.Service;

namespace WayfarerAPI.Api.Controllers;

[Route("api/travel/{travelId:guid}/itinerary")]
[Authorize]
public class ItineraryController : ApiControllerBase
{
    private readonly IItineraryService _itineraryService;

    public ItineraryController(IItineraryService itineraryService)
    {
        _itineraryService = itineraryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetByTravel(Guid travelId)
    {
        var travellerId = GetCurrentUserId();
        return ApiResult(await _itineraryService.GetByTravelIdAsync(travelId, travellerId));
    }

    [HttpGet("ai-itinerary")]
    public async Task<IActionResult> GetAIGeneratedItinerary(Guid travelId, string userPreferences)
    {
        var travellerId = GetCurrentUserId();
        return ApiResult(await _itineraryService.GenerateItineraryByAI(travelId, userPreferences, CancellationToken.None));
    }

    [HttpPut]
    public async Task<IActionResult> UpsertDays(
        Guid travelId,
        [FromBody] UpsertItineraryBatchRequestDto request)
    {
        var travellerId = GetCurrentUserId();
        var result = await _itineraryService.UpsertDaysAsync(travelId, travellerId, request);
        return ApiResult(result);
    }
}
