using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.Service;
using WayfarerAPI.Application.Interfaces.Utilities;

namespace WayfarerAPI.Application.Services;

public sealed class GoogleCalendarService : IGoogleCalendarService
{
    private readonly IGoogleCalendarClient _googleCalendarClient;

    public GoogleCalendarService(IGoogleCalendarClient googleCalendarClient)
    {
        _googleCalendarClient = googleCalendarClient;
    }

    public async Task<List<GoogleCalendarEventDto>> GetEventsAsync(string accessToken, DateTime timeMin, DateTime timeMax)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Google access token is required.");

        return await _googleCalendarClient.GetEventsAsync(accessToken, timeMin, timeMax);
    }

    public async Task<GoogleCalendarEventDto> PushEventAsync(SyncToGoogleCalendarRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
            throw new ArgumentException("Google access token is required.");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Event title is required.");

        var calendarEvent = new GoogleCalendarEventDto
        {
            Title = request.Title,
            Description = request.Description,
            Location = request.Location,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsAllDay = request.IsAllDay
        };

        return await _googleCalendarClient.CreateEventAsync(request.AccessToken, calendarEvent);
    }

    public async Task<GoogleCalendarEventDto> UpdateEventAsync(string accessToken, string googleEventId, SyncToGoogleCalendarRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Google access token is required.");

        if (string.IsNullOrWhiteSpace(googleEventId))
            throw new ArgumentException("Google event ID is required.");

        var calendarEvent = new GoogleCalendarEventDto
        {
            Title = request.Title,
            Description = request.Description,
            Location = request.Location,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsAllDay = request.IsAllDay
        };

        return await _googleCalendarClient.UpdateEventAsync(accessToken, googleEventId, calendarEvent);
    }

    public async Task DeleteEventAsync(string accessToken, string googleEventId)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Google access token is required.");

        if (string.IsNullOrWhiteSpace(googleEventId))
            throw new ArgumentException("Google event ID is required.");

        await _googleCalendarClient.DeleteEventAsync(accessToken, googleEventId);
    }
}
