using WayfarerAPI.Application.Models;

namespace WayfarerAPI.Application.Interfaces.QueryServices;

public interface ITravelQueryService
{
    Task<TravellerInfoModel?> GetTravellerInfoByTravellerIdAsync(Guid travellerId);
    Task<TravellerInfoModel?> GetTravellerInfoByEmailAsync(string email);
    Task<IEnumerable<TravellerInfoModel>> GetFriendInfoByTravellerIdAsync(Guid travellerId);
    Task<IEnumerable<TravelSummaryModel>> GetAllSummaryByTravellerIdAsync(Guid travellerId);
}
