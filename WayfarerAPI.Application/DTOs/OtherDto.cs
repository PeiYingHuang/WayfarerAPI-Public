namespace WayfarerAPI.Application.DTOs;

// Exchange Rate

/// <summary>
/// Exchange Rate DTO - 匯率資訊
/// </summary>
public sealed class ExchangeRateDto
{
    public string ConsumptionCurrencyCode { get; set; } = string.Empty;
    public string SettlementCurrencyCode { get; set; } = string.Empty;
    public decimal Rate { get; set; }
}

// Google Calendar

/// <summary>
/// Google Calendar Event DTO - Google 日曆事件資訊
/// </summary>
public sealed class GoogleCalendarEventDto
{
    public string? GoogleEventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsAllDay { get; set; }
}

/// <summary>
/// Sync To Google Calendar Request DTO - 同步至 Google 日曆請求
/// </summary>
public sealed class SyncToGoogleCalendarRequestDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAllDay { get; set; }
}

// OCR Receipt

/// <summary>
/// OCR Receipt DTO - 收據 OCR 識別結果
/// </summary>
public sealed class OcrReceiptDto
{
    public string? MerchantName { get; set; }
    public string? ConsumedAt { get; set; }
    public decimal? TotalAmount { get; set; }
    public List<OcrReceiptItemDto> Items { get; set; } = [];
}

/// <summary>
/// OCR Receipt Item DTO - 收據 OCR 識別項目
/// </summary>
public sealed class OcrReceiptItemDto
{
    public string Name { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public decimal? Amount { get; set; }
    /// <summary>
    /// 中文說明
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
