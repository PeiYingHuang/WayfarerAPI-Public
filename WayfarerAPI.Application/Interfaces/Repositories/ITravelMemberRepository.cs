using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Application.Interfaces.Repositories;

public interface ITravelMemberRepository
{
    Task<TravelMember?> GetByIdAsync(Guid memberId);
    Task<IEnumerable<TravelMember>> GetByTravelIdAsync(Guid travelId);
    Task<IEnumerable<TravelMember>> GetByTravelIdsAsync(IEnumerable<Guid> travelIds);
    Task<IEnumerable<TravelMember>> GetByTravellerIdAsync(Guid travellerId);
    Task InsertAsync(TravelMember member);
    Task UpdateAsync(TravelMember member);
    Task DeleteByIdsAsync(Guid travelId, IEnumerable<Guid> memberIds);
    Task DeleteByTravelIdAsync(Guid travelId);
}
