namespace WayfarerAPI.Domain.Entities;

public class RefreshToken
{
    public long Id { get; set; }
    public Guid TravellerId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
