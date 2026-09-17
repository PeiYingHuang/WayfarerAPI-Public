using System.Text.Json.Serialization;

namespace WayfarerAPI.Application.Models;

/// <summary>
/// OpenAI Vision API 回應模型
/// </summary>
public class OpenAiOcrModel
{
    [JsonPropertyName("choices")]
    public required List<Choice> Choices { get; set; }
}

/// <summary>
/// OpenAI API 回應中的每一個 choice 選項
/// </summary>
public class Choice
{
    [JsonPropertyName("message")]
    public required Message Message { get; set; }
}

/// <summary>
/// OpenAI API 回應中的訊息內容
/// </summary>
public class Message
{
    [JsonPropertyName("content")]
    public required string Content { get; set; }
}

public class OpenAiOcrReceiptModel
{
    [JsonPropertyName("choices")]
    public required List<Choice> Choices { get; set; }
}

public class ReceiptModel
{
    [JsonPropertyName("merchantName")]
    public string? MerchantName { get; set; }

    [JsonPropertyName("consumedAt")]
    public DateTime? ConsumedAt { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal? TotalAmount { get; set; }

    [JsonPropertyName("items")]
    public List<ReceiptItemModel> Items { get; set; } = new();
}

public class ReceiptItemModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; set; }

    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}