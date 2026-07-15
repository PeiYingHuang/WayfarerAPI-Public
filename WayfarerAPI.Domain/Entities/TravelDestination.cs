namespace WayfarerAPI.Domain.Entities
{
    public class TravelDestination
    {
        public Guid Id { get; set; }
        public Guid TravelId { get; set; }
        public int DestinationId { get; set; }
        public int SortOrder { get; set; }
    }
}
