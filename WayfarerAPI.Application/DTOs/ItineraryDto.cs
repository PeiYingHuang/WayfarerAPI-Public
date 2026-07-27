using WayfarerAPI.Domain.Enumerations;

namespace WayfarerAPI.Application.DTOs;

// Response DTOs

/// <summary>
/// Itinerary Day Response DTO - 行程日期資訊回應
/// </summary>
public sealed class ItineraryDayDto
{
    public Guid Id { get; set; }
    public Guid TravelId { get; set; }
    public int DayNumber { get; set; }
    public string TravelDate { get; set; } = string.Empty;   // YYYY-MM-DD
    public string? DayTitle { get; set; }
    public List<ItineraryDetailDto> Details { get; set; } = [];
}

/// <summary>
/// Itinerary Detail Response DTO - 行程細項資訊回應
/// </summary>
public sealed class ItineraryDetailDto
{
    public Guid Id { get; set; }
    public Guid ItineraryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? StartTime { get; set; }   // HH:mm
    public string? EndTime { get; set; }     // HH:mm
    public string? LocationName { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public ItineraryCategoryEnum? Category { get; set; }
    public int SortOrder { get; set; }
}

// Request DTOs

/// <summary>
/// Upsert Itinerary Batch Request DTO - 批量新增或更新行程請求
/// </summary>
public sealed class UpsertItineraryBatchRequestDto
{
    public List<UpsertItineraryDayRequestDto> Days { get; set; } = [];
}

/// <summary>
/// Upsert Itinerary Day Request DTO - 新增或更新行程日期請求
/// </summary>
public sealed class UpsertItineraryDayRequestDto
{
    public string Date { get; set; } = string.Empty;   // yyyy-MM-dd
    public int DayNumber { get; set; }
    public string? DayTitle { get; set; }
    public List<UpsertItineraryDetailDto> Details { get; set; } = [];
}

/// <summary>
/// Upsert Itinerary Detail Request DTO - 新增或更新行程細項請求
/// </summary>
public sealed class UpsertItineraryDetailDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? StartTime { get; set; }   // HH:mm
    public string? EndTime { get; set; }     // HH:mm
    public string? LocationName { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? Category { get; set; }
    public int SortOrder { get; set; }
}
