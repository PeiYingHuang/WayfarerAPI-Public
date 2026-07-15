using WayfarerAPI.Application.DTOs;

namespace WayfarerAPI.Application.Interfaces.Service;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto> RefreshAsync(RefreshTokenRequestDto request);
    Task UpdateProfileAsync(Guid travellerId, UpdateProfileRequestDto request);
    Task ChangePasswordAsync(Guid travellerId, ChangePasswordRequestDto request);
}
