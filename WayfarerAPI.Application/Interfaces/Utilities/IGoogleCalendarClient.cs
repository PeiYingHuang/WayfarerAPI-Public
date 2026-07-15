using WayfarerAPI.Application.DTOs;

namespace WayfarerAPI.Application.Interfaces.Utilities;

public interface IGoogleCalendarClient
{
    /// <summary>
    /// 從 Google Calendar 讀取事件
    /// </summary>
    Task<List<GoogleCalendarEventDto>> GetEventsAsync(string accessToken, DateTime timeMin, DateTime timeMax);

    /// <summary>
    /// 新增事件到 Google Calendar
    /// </summary>
    Task<GoogleCalendarEventDto> CreateEventAsync(string accessToken, GoogleCalendarEventDto calendarEvent);

    /// <summary>
    /// 更新 Google Calendar 上的事件
    /// </summary>
    Task<GoogleCalendarEventDto> UpdateEventAsync(string accessToken, string googleEventId, GoogleCalendarEventDto calendarEvent);

    /// <summary>
    /// 刪除 Google Calendar 上的事件
    /// </summary>
    Task DeleteEventAsync(string accessToken, string googleEventId);
}
