using WayfarerAPI.Application.DTOs;

namespace WayfarerAPI.Application.Interfaces.Service;

public interface IItineraryService
{
    Task<List<ItineraryDayDto>> GetByTravelIdAsync(Guid travelId, Guid travellerId);
    Task<List<ItineraryDayDto>> UpsertDaysAsync(Guid travelId, Guid travellerId, UpsertItineraryBatchRequestDto request);
    Task<List<ItineraryDayDto>> GenerateItineraryByAI(Guid travelId, string userPreferences, CancellationToken ct);
}
