using WayfarerAPI.Application.Models;
using WayfarerAPI.Domain.Enumerations;

namespace WayfarerAPI.Application.Interfaces.Utilities;

public interface IOpenAiVisionClient
{
    Task<string> ParseReceiptAsync(byte[] imageBytes, string mimeType, string? currency = null);
    Task<AiItineraryDraftModel> GenerateItineraryAsync(ItineraryAiModel request, CancellationToken ct);
    Task<ItineraryCategoryEnum> ParseCategory(string? promptValue);
}