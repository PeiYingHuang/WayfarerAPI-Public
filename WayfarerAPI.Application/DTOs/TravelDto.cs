namespace WayfarerAPI.Application.DTOs;

public sealed class TravellerResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
}
/// <summary>
/// Travel Response DTO - 旅程回應資料
/// </summary>
public sealed class TravelResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string ConsumptionCurrencyCode { get; set; } = "TWD";
    public string SettlementCurrencyCode { get; set; } = "TWD";
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TravelFlightDto> Flights { get; set; } = [];
    public List<TravelMemberDto> Members { get; set; } = [];
}

/// <summary>
/// Travel Flight DTO - 航班資訊
/// </summary>
public sealed class TravelFlightDto
{
    public Guid Id { get; set; }
    public string? FlightNumber { get; set; }
    public string? DepartureAirport { get; set; }
    public string? ArrivalAirport { get; set; }
    public string? DepartureAt { get; set; }
    public string? ArrivalAt { get; set; }
    public string Direction { get; set; } = string.Empty;
}

/// <summary>
/// Travel Member DTO - 旅行成員資訊
/// </summary>
public sealed class TravelMemberDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? TravellerId { get; set; }
    public string MemberType { get; set; } = string.Empty;
    public int? Age { get; set; }
    public bool IsPayer { get; set; }
}

/// <summary>
/// Travel Summary DTO - 旅程摘要資料（輕量級回應）
/// </summary>
public sealed class TravelSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int AdultCount { get; set; }
    public int ChildCount { get; set; }
    public int FlightCount { get; set; }
}

/// <summary>
/// Upsert Travel Request DTO - 新增或更新旅程請求
/// </summary>
public sealed class UpsertTravelRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string ConsumptionCurrencyCode { get; set; } = "TWD";
    public string SettlementCurrencyCode { get; set; } = "TWD";
    public List<UpsertTravelFlightDto> Flights { get; set; } = [];
    public List<UpsertTravelMemberDto> Members { get; set; } = [];
}

/// <summary>
/// Upsert Travel Flight DTO - 新增或更新航班請求
/// </summary>
public sealed class UpsertTravelFlightDto
{
    public string? FlightNumber { get; set; }
    public string? DepartureAirport { get; set; }
    public string? ArrivalAirport { get; set; }
    public DateTime? DepartureAt { get; set; }
    public DateTime? ArrivalAt { get; set; }
    public string Direction { get; set; } = "Outbound";
}

/// <summary>
/// Upsert Travel Member DTO - 新增或更新旅行成員請求
/// </summary>
public sealed class UpsertTravelMemberDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? TravellerId { get; set; }
    public string MemberType { get; set; } = "Adult";
    public int? Age { get; set; }
    public bool IsPayer { get; set; } = true;
}
