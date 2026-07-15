using WayfarerAPI.Application.DTOs;

namespace WayfarerAPI.Application.Interfaces.Service;

public interface IGoogleCalendarService
{
    /// <summary>
    /// 取得指定時間範圍的 Google Calendar 事件
    /// </summary>
    Task<List<GoogleCalendarEventDto>> GetEventsAsync(string accessToken, DateTime timeMin, DateTime timeMax);

    /// <summary>
    /// 將行程推送到 Google Calendar
    /// </summary>
    Task<GoogleCalendarEventDto> PushEventAsync(SyncToGoogleCalendarRequestDto request);

    /// <summary>
    /// 更新 Google Calendar 上的事件
    /// </summary>
    Task<GoogleCalendarEventDto> UpdateEventAsync(string accessToken, string googleEventId, SyncToGoogleCalendarRequestDto request);

    /// <summary>
    /// 刪除 Google Calendar 上的事件
    /// </summary>
    Task DeleteEventAsync(string accessToken, string googleEventId);
}
