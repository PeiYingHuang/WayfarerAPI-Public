using WayfarerAPI.Application.DTOs;

namespace WayfarerAPI.Application.Interfaces.Services;

public interface ITravellerService
{
    Task<TravellerResponseDto?> GetTravellerInfoByEmailAsync(string email);
}
