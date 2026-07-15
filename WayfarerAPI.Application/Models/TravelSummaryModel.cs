namespace WayfarerAPI.Application.Models;

public class TravelSummaryModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int AdultCount { get; set; }
    public int ChildCount { get; set; }
    public int FlightCount { get; set; }
}
