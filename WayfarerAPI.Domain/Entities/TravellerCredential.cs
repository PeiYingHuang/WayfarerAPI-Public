namespace WayfarerAPI.Domain.Entities;

public class TravellerCredential
{
    public Guid Id { get; set; }
    public Guid TravellerId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderUserId { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordAlgo { get; set; } = string.Empty;
}
