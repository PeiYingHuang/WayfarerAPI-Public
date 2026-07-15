namespace WayfarerAPI.Application.Models
{
    public class ItineraryAiModel
    {
        public string Destination { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int AdultCount { get; set; }
        public List<(int? Age, int Count)> Children { get; set; } = new();
        public FlightInfoModel? OutboundFlight { get; set; }
        public FlightInfoModel? ReturnFlight { get; set; }
        public string UserPreferences { get; set; } = string.Empty;
    }

    public class FlightInfoModel
    {
        public string FlightNumber { get; set; } = string.Empty;
        public DateTime? DepartureAt { get; set; }
        public DateTime? ArrivalAt { get; set; }
        public string DepartureAirport { get; set; } = string.Empty;
        public string ArrivalAirport { get; set; } = string.Empty;
    }

    public sealed class AiItineraryDraftModel
    {
        public List<AiItineraryDayDraftModel> Days { get; set; } = [];
    }

    public sealed class AiItineraryDayDraftModel
    {
        public int DayNumber { get; set; }
        public DateTime Date { get; set; }
        public string? DayTitle { get; set; }
        public List<AiItineraryDetailDraftModel> Details { get; set; } = [];
    }

    public sealed class AiItineraryDetailDraftModel
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? LocationName { get; set; }
        public string? Category { get; set; }
    }
}
