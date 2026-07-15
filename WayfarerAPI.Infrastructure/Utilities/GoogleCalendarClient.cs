using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.Utilities;

namespace WayfarerAPI.Infrastructure.Utilities;

public sealed class GoogleCalendarClient : IGoogleCalendarClient
{
    private const string CalendarId = "primary";

    public async Task<List<GoogleCalendarEventDto>> GetEventsAsync(string accessToken, DateTime timeMin, DateTime timeMax)
    {
        using var service = CreateCalendarService(accessToken);

        var request = service.Events.List(CalendarId);
        request.TimeMinDateTimeOffset = new DateTimeOffset(timeMin, TimeSpan.Zero);
        request.TimeMaxDateTimeOffset = new DateTimeOffset(timeMax, TimeSpan.Zero);
        request.SingleEvents = true;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        var events = await request.ExecuteAsync();

        return events.Items?.Select(MapToDto).ToList() ?? [];
    }

    public async Task<GoogleCalendarEventDto> CreateEventAsync(string accessToken, GoogleCalendarEventDto calendarEvent)
    {
        using var service = CreateCalendarService(accessToken);

        var googleEvent = MapToGoogleEvent(calendarEvent);
        var createdEvent = await service.Events.Insert(googleEvent, CalendarId).ExecuteAsync();

        return MapToDto(createdEvent);
    }

    public async Task<GoogleCalendarEventDto> UpdateEventAsync(string accessToken, string googleEventId, GoogleCalendarEventDto calendarEvent)
    {
        using var service = CreateCalendarService(accessToken);

        var googleEvent = MapToGoogleEvent(calendarEvent);
        var updatedEvent = await service.Events.Update(googleEvent, CalendarId, googleEventId).ExecuteAsync();

        return MapToDto(updatedEvent);
    }

    public async Task DeleteEventAsync(string accessToken, string googleEventId)
    {
        using var service = CreateCalendarService(accessToken);

        await service.Events.Delete(CalendarId, googleEventId).ExecuteAsync();
    }

    private static CalendarService CreateCalendarService(string accessToken)
    {
        var credential = GoogleCredential.FromAccessToken(accessToken);

        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Wayfarer"
        });
    }

    private static GoogleCalendarEventDto MapToDto(Event googleEvent)
    {
        var isAllDay = googleEvent.Start?.DateTimeDateTimeOffset == null;

        return new GoogleCalendarEventDto
        {
            GoogleEventId = googleEvent.Id,
            Title = googleEvent.Summary ?? string.Empty,
            Description = googleEvent.Description,
            Location = googleEvent.Location,
            StartTime = googleEvent.Start?.DateTimeDateTimeOffset?.LocalDateTime,
            EndTime = googleEvent.End?.DateTimeDateTimeOffset?.LocalDateTime,
            IsAllDay = isAllDay
        };
    }

    private static Event MapToGoogleEvent(GoogleCalendarEventDto dto)
    {
        var googleEvent = new Event
        {
            Summary = dto.Title,
            Description = dto.Description,
            Location = dto.Location
        };

        if (dto.IsAllDay)
        {
            googleEvent.Start = new EventDateTime { Date = dto.StartTime?.ToString("yyyy-MM-dd") };
            googleEvent.End = new EventDateTime { Date = dto.EndTime?.ToString("yyyy-MM-dd") };
        }
        else
        {
            googleEvent.Start = new EventDateTime { DateTimeDateTimeOffset = dto.StartTime.HasValue ? new DateTimeOffset(dto.StartTime.Value, TimeSpan.Zero) : null };
            googleEvent.End = new EventDateTime { DateTimeDateTimeOffset = dto.EndTime.HasValue ? new DateTimeOffset(dto.EndTime.Value, TimeSpan.Zero) : null };
        }

        return googleEvent;
    }
}
