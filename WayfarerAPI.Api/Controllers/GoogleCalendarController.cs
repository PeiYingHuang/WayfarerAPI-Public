using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.Service;

namespace WayfarerAPI.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
public class GoogleCalendarController : ApiControllerBase
{
    private readonly IGoogleCalendarService _googleCalendarService;

    public GoogleCalendarController(IGoogleCalendarService googleCalendarService)
    {
        _googleCalendarService = googleCalendarService;
    }

    /// <summary>
    /// 從 Google Calendar 讀取事件
    /// </summary>
    [HttpGet("events")]
    public async Task<IActionResult> GetEvents(
        [FromHeader(Name = "GcalToken")] string? accessToken,
        [FromQuery] DateTime timeMin,
        [FromQuery] DateTime timeMax)
    {
        var events = await _googleCalendarService.GetEventsAsync(accessToken ?? string.Empty, timeMin, timeMax);
        return ApiResult(events);
    }

    /// <summary>
    /// 將行程推送到 Google Calendar
    /// </summary>
    [HttpPost("events")]
    public async Task<IActionResult> PushEvent([FromBody] SyncToGoogleCalendarRequestDto request)
    {
        var result = await _googleCalendarService.PushEventAsync(request);
        return ApiResult(result);
    }

    /// <summary>
    /// 更新 Google Calendar 上的事件
    /// </summary>
    [HttpPut("events/{googleEventId}")]
    public async Task<IActionResult> UpdateEvent(
        [FromHeader(Name = "GcalToken")] string? accessToken,
        string googleEventId,
        [FromBody] SyncToGoogleCalendarRequestDto request)
    {
        var result = await _googleCalendarService.UpdateEventAsync(accessToken ?? string.Empty, googleEventId, request);
        return ApiResult(result);
    }

    /// <summary>
    /// 刪除 Google Calendar 上的事件
    /// </summary>
    [HttpDelete("events/{googleEventId}")]
    public async Task<IActionResult> DeleteEvent(
        [FromHeader(Name = "GcalToken")] string? accessToken,
        string googleEventId)
    {
        await _googleCalendarService.DeleteEventAsync(accessToken ?? string.Empty, googleEventId);
        return ApiResult(true);
    }
}
