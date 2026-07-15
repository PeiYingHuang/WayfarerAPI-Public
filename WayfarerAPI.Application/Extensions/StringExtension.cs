namespace WayfarerAPI.Application.Extensions;

public static class StringExtension
{
    public static TimeSpan? ToTimeSpanOrNull(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return TimeSpan.TryParse(value, out var time) ? time : null;
    }
}
