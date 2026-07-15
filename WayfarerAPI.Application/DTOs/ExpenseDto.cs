namespace WayfarerAPI.Application.DTOs;

/// <summary>
/// Expense Response DTO - 花費回應資料
/// </summary>
public class ExpenseResponseDto
{
    public Guid Id { get; set; }
    public Guid TravelId { get; set; }
    public Guid PayerMemberId { get; set; }
    public string PayerName { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal ConsumptionAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal SettlementAmount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTime ExpenseTime { get; set; }
    public List<string> ReceiptUrls { get; set; } = new();
    public List<ExpenseDetailResponseDto> Details { get; set; } = new();
    public List<ExpenseSplitDto> Splits { get; set; } = new();
}

/// <summary>
/// Expense Summary DTO - 花費摘要資料（輕量級回應）
/// </summary>
public class ExpenseSummaryDto
{
    public Guid Id { get; set; }
    public Guid TravelId { get; set; }
    public string PayerMemberName { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal ConsumptionAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal SettlementAmount { get; set; }
    public string? Category { get; set; }
    public DateTime ExpenseTime { get; set; }
    public int SplitMemberNumber { get; set; }
}

/// <summary>
/// Expense Detail Response DTO - 花費細項回應資料
/// </summary>
public class ExpenseDetailResponseDto
{
    public int Id { get; set; }
    public Guid ExpenseId { get; set; }
    public string Item { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<ExpenseDetailSplitDto>? DetailSplits { get; set; }
}

/// <summary>
/// Expense Split DTO - 花費分帳資訊
/// </summary>
public class ExpenseSplitDto
{
    public Guid Id { get; set; }
    public Guid ExpenseId { get; set; }
    public Guid MemberId { get; set; }
    public decimal SplitAmount { get; set; }
}

/// <summary>
/// Expense Detail Split DTO - 花費細項分帳資訊
/// </summary>
public class ExpenseDetailSplitDto
{
    public int Id { get; set; }
    public int ExpenseDetailId { get; set; }
    public Guid MemberId { get; set; }
    public decimal SplitAmount { get; set; }
}

public class SettlementTransactionResponseDto
{
    public Guid FromMemberId { get; set; }
    public string FromMemberName { get; set; } = string.Empty;
    public Guid ToMemberId { get; set; }
    public string ToMemberName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

/// <summary>
/// Member Expense Item DTO - 成員花費明細項目
/// </summary>
public class MemberExpenseItemDto
{
    public Guid ExpenseId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime ExpenseTime { get; set; }
}

/// <summary>
/// Member Expense DTO - 成員花費資訊
/// </summary>
public class MemberExpenseDto
{
    public Guid MemberId { get; set; }
    public Guid ExpenseId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? ExpenseTime { get; set; }
    public string? Category { get; set; }
}

/// <summary>
/// Create Expense Request DTO - 建立花費請求
/// </summary>
public sealed class CreateExpenseRequestDto
{
    public Guid TravelId { get; set; }
    public Guid? ItineraryDetailId { get; set; }
    public Guid PayerMemberId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal ConsumptionAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal SettlementAmount { get; set; }
    public string? Category { get; set; }
    public string? Note { get; set; }
    public DateTime? ExpenseTime { get; set; }
    public List<ExpenseDetailRequestDto> Details { get; set; } = new();
    public List<ExpenseSplitRequestDto> Splits { get; set; } = new();
}

/// <summary>
/// Expense Detail Request DTO - 建立花費細項請求
/// </summary>
public sealed class ExpenseDetailRequestDto
{
    public string Item { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    /// <summary>
    /// 可選的細項分帳
    /// </summary>
    public List<ExpenseDetailSplitDto>? DetailSplits { get; set; }
}

/// <summary>
/// Expense Split Request DTO - 建立花費分帳請求
/// </summary>
public sealed class ExpenseSplitRequestDto
{
    public Guid MemberId { get; set; }
    public decimal SplitAmount { get; set; }
}

/// <summary>
/// Update Expense Request DTO - 更新花費請求
/// </summary>
public sealed class UpdateExpenseRequestDto
{
    public Guid PayerMemberId { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal SettlementAmount { get; set; }
    public decimal ConsumptionAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Note { get; set; }
    public DateTime? ExpenseTime { get; set; }
    public string? ItemName { get; set; }
    public List<ExpenseDetailRequestDto> Details { get; set; } = new();
    public List<ExpenseSplitRequestDto> Splits { get; set; } = new();
}
