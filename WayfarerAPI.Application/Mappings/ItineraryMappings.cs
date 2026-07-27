using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Extensions;
using WayfarerAPI.Application.Models;
using WayfarerAPI.Domain.Entities;
using WayfarerAPI.Domain.Enumerations;

namespace WayfarerAPI.Application.Mappings;

public static class ItineraryMappings
{
    public static ItineraryDetail ToItineraryDetail(UpsertItineraryDetailDto dto, Guid itineraryId) => new()
    {
        Id = Guid.CreateVersion7(),
        ItineraryId = itineraryId,
        Title = dto.Title.Trim(),
        Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
        StartTime = dto.StartTime.ToTimeSpanOrNull(),
        EndTime = dto.EndTime.ToTimeSpanOrNull(),
        LocationName = string.IsNullOrWhiteSpace(dto.LocationName) ? null : dto.LocationName.Trim(),
        Latitude = dto.Latitude,
        Longitude = dto.Longitude,
        Category = dto.Category.HasValue ? Enum.TryParse<ItineraryCategoryEnum>(dto.Category.Value.ToString(), out var category) ? category : (ItineraryCategoryEnum?)null : null,
        SortOrder = dto.SortOrder,
    };

    public static ItineraryDayDto ToDto(Itinerary day, IEnumerable<ItineraryDetail> details) => new()
    {
        Id = day.Id,
        TravelId = day.TravelId,
        DayNumber = day.DayNumber,
        TravelDate = day.TravelDate.ToString("yyyy-MM-dd"),
        DayTitle = day.DayTitle,
        Details = details
            .OrderBy(d => d.SortOrder)
            .Select(d => new ItineraryDetailDto
            {
                Id = d.Id,
                ItineraryId = d.ItineraryId,
                Title = d.Title,
                Description = d.Description,
                StartTime = d.StartTime?.ToString(@"hh\:mm"),
                EndTime = d.EndTime?.ToString(@"hh\:mm"),
                LocationName = d.LocationName,
                Latitude = d.Latitude,
                Longitude = d.Longitude,
                Category = d.Category,
                SortOrder = d.SortOrder,
            })
            .ToList(),
    };
}
