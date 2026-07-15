namespace WayfarerAPI.Domain.Entities;

public class TravelMember
{
    public Guid Id { get; set; }
    public Guid TravelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? TravellerId { get; set; }
    public string MemberType { get; set; } = "Adult";
    public int? Age { get; set; }
    public bool IsPayer { get; set; } = true;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
