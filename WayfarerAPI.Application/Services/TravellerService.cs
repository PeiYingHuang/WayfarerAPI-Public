using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.QueryServices;
using WayfarerAPI.Application.Interfaces.Services;
using WayfarerAPI.Application.Mappings;

namespace WayfarerAPI.Application.Services;

public sealed class TravellerService : ITravellerService
{
    private readonly ITravelQueryService _travelQueryService;

    public TravellerService(ITravelQueryService travelQueryService)
    {
        _travelQueryService = travelQueryService;
    }

    public async Task<TravellerResponseDto?> GetTravellerInfoByEmailAsync(string email)
    {
       var travellerInfo = await _travelQueryService.GetTravellerInfoByEmailAsync(email);
       return TravelMappings.ToTravellerResponseDto(travellerInfo);
    }
}
