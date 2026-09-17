using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.Service;
using WayfarerAPI.Application.Interfaces.Utilities;
using WayfarerAPI.Application.Mappings;

namespace WayfarerAPI.Application.Services;

public sealed class OcrService : IOcrService
{
    private readonly ILogger<OcrService> _logger;
    private readonly IOpenAiVisionClient _openAiVisionClient;

    public OcrService(IOpenAiVisionClient openAiVisionClient, ILogger<OcrService> logger)
    {
        _openAiVisionClient = openAiVisionClient;
        _logger = logger;
    }

    public async Task<OcrReceiptDto> ParseReceiptAsync(byte[] imageBytes, string? contentType, string? currency = null)
    {
        if (imageBytes.Length == 0)
            throw new ArgumentException("請上傳圖片");

        var mimeType = NormalizeMimeType(contentType);
        var normalizedCurrency = NormalizeCurrency(currency);
        var receipt = await _openAiVisionClient.ParseReceiptAsync(imageBytes, mimeType, normalizedCurrency);
        _logger.LogInformation("receipt: {0}", JsonConvert.SerializeObject(receipt));
        return ReceiptMappings.ToDto(receipt);
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
}
