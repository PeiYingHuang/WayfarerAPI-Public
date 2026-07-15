using WayfarerAPI.Domain.Enumerations;

namespace WayfarerAPI.Domain.Entities;

public class ItineraryDetail
{
    public Guid Id { get; set; }
    public Guid ItineraryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? LocationName { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public ItineraryCategoryEnum? Category { get; set; }
    public int SortOrder { get; set; }
}
