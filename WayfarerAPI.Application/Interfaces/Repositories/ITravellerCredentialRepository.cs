using WayfarerAPI.Domain.Entities;
using WayfarerAPI.Domain.Enumerations;

namespace WayfarerAPI.Application.Interfaces.Repositories;

public interface ITravellerCredentialRepository
{
    Task<TravellerCredential?> GetByProviderAsync(Guid travellerId, PasswordProviderEnum passwordProvider);
    Task<TravellerCredential?> GetByEmailAsync(string email);
    Task InsertAsync(TravellerCredential credential);
    Task UpdateAsync(TravellerCredential credential);
}
