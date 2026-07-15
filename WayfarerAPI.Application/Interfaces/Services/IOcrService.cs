using WayfarerAPI.Application.DTOs;

namespace WayfarerAPI.Application.Interfaces.Service;

public interface IOcrService
{
    Task<OcrReceiptDto> ParseReceiptAsync(byte[] imageBytes, string? contentType, string? currency = null);
}
