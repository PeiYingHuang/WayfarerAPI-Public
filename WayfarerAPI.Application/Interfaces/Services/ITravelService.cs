using WayfarerAPI.Application.DTOs;

namespace WayfarerAPI.Application.Interfaces.Service;

public interface ITravelService
{
    Task<TravelResponseDto> CreateAsync(Guid travellerId, UpsertTravelRequestDto request);
    Task<TravelResponseDto> UpdateAsync(Guid travelId, Guid travellerId, UpsertTravelRequestDto request);
    Task<List<TravelResponseDto>> GetAllByTravellerAsync(Guid travellerId);
    Task<List<TravelSummaryDto>> GetAllSummaryByTravellerAsync(Guid travellerId);
    Task<TravelResponseDto?> GetByIdAsync(Guid travelId, Guid travellerId);
    Task DeleteAsync(Guid travelId, Guid travellerId);
    Task<IEnumerable<TravellerResponseDto>> GetTravellerInfoByIdAsync(Guid travellerId);
    Task<IEnumerable<TravellerResponseDto>> GetFriendsByTravellerIdAsync(Guid travellerId);
}
