using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task<long> CreateAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetValidByHashAsync(string tokenHash);
    Task RevokeAsync(long refreshTokenId);
}
