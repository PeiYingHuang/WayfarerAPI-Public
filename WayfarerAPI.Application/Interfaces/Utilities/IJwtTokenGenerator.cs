using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Application.Interfaces.Utilities;

public interface IJwtTokenGenerator
{
    (string token, DateTime expiresAtUtc) Generate(Traveller user);
}
