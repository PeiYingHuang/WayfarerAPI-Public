using WayfarerAPI.Domain.Enumerations;

namespace WayfarerAPI.Domain.Entities
{
    public class ConfigDestination
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public TravelDestinationTypeEnum Type { get; set; } = TravelDestinationTypeEnum.Country;
        public string CountryCode { get; set; } = string.Empty;
        public int? ParentId { get; set; }
    }
}
