namespace WayfarerAPI.Domain.Entities;

public class Itinerary
{
    public Guid Id { get; set; }
    public Guid TravelId { get; set; }
    public int DayNumber { get; set; }
    public DateTime TravelDate { get; set; }
    public string? DayTitle { get; set; }
}
