namespace WayfarerAPI.Domain.Entities;

public class TravelFlight
{
    public Guid Id { get; set; }
    public Guid TravelId { get; set; }
    public string? FlightNumber { get; set; }
    public string? DepartureAirport { get; set; }
    public string? ArrivalAirport { get; set; }
    public DateTime? DepartureAt { get; set; }
    public DateTime? ArrivalAt { get; set; }
    public string Direction { get; set; } = "Outbound";
    public DateTime CreatedAt { get; set; }
}
