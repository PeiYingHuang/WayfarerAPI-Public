using System.Globalization;
using System.Text.Json;
using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.Service;
using WayfarerAPI.Application.Interfaces.Utilities;

namespace WayfarerAPI.Application.Services;

public sealed class OcrService : IOcrService
{
    private readonly IOpenAiVisionClient _openAiVisionClient;

    public OcrService(IOpenAiVisionClient openAiVisionClient)
    {
        _openAiVisionClient = openAiVisionClient;
    }

    public async Task<OcrReceiptDto> ParseReceiptAsync(byte[] imageBytes, string? contentType, string? currency = null)
    {
        if (imageBytes.Length == 0)
            throw new ArgumentException("請上傳圖片");

        var mimeType = NormalizeMimeType(contentType);
        var normalizedCurrency = NormalizeCurrency(currency);
        var jsonText = await _openAiVisionClient.ParseReceiptAsync(imageBytes, mimeType, normalizedCurrency);

        using var doc = JsonDocument.Parse(jsonText);
        var root = doc.RootElement;

        var dto = new OcrReceiptDto
        {
            MerchantName = GetString(root, "merchantName"),
            ConsumedAt = NormalizeDateTimeString(GetString(root, "consumedAt")),
            TotalAmount = GetDecimal(root, "totalAmount"),
            Items = GetItems(root)
        };

        return dto;
    }

    private static string NormalizeMimeType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return "image/jpeg";

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" or "image/png" or "application/pdf" => contentType,
            _ => "image/jpeg"
        };
    }

    private static string? NormalizeCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return null;

        var code = currency.Trim().ToUpperInvariant();
        return code.Length == 3 ? code : null;
    }

    private static string? NormalizeDateTimeString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTime.TryParse(value, out var dt)
            ? dt.ToString("yyyy-MM-ddTHH:mm")
            : value;
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
            return null;

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.ToString(),
            _ => null
        };
    }

    private static decimal? GetDecimal(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
            return null;

        return ParseDecimal(element);
    }

    private static List<OcrReceiptItemDto> GetItems(JsonElement root)
    {
        if (!root.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array)
            return [];

        var items = new List<OcrReceiptItemDto>();

        foreach (var itemElement in itemsElement.EnumerateArray())
        {
            if (itemElement.ValueKind != JsonValueKind.Object)
                continue;

            var name = GetString(itemElement, "name");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            items.Add(new OcrReceiptItemDto
            {
                Name = name.Trim(),
                Quantity = GetDecimal(itemElement, "quantity"),
                Amount = GetDecimal(itemElement, "amount"),
                Description = GetString(itemElement, "description") ?? string.Empty
            });
        }

        return items;
    }

    private static decimal? ParseDecimal(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var number))
            return number;

        if (element.ValueKind == JsonValueKind.String)
        {
            var raw = element.GetString();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                var normalized = new string(raw.Where(c => char.IsDigit(c) || c is '.' or ',' or '-').ToArray())
                    .Replace(",", "");

                if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                    return parsed;
            }
        }

        return null;
    }
}
