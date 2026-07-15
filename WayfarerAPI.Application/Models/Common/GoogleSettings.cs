namespace WayfarerAPI.Application.Models.Common;

public sealed class GoogleSettings
{
    public const string SectionName = "Google";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
