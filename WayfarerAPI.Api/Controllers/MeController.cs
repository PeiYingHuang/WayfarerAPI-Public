using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.Service;

namespace WayfarerAPI.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
public class MeController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITravelService _travelService;
    public MeController(IAuthService authService, ITravelService travelService)
    {
        _authService = authService;
        _travelService = travelService;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        var travellerId = GetCurrentUserId();
        return ApiResult(await _travelService.GetTravellerInfoByIdAsync(travellerId));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        var travellerId = GetCurrentUserId();
        await _authService.UpdateProfileAsync(travellerId, request);
        return ApiResult(true);
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var travellerId = GetCurrentUserId();
        await _authService.ChangePasswordAsync(travellerId, request);
        return ApiResult(true);
    }

    [HttpGet("friends")]
    public async Task<IActionResult> GetFriends()
    {
        var travellerId = GetCurrentUserId();
        return ApiResult(await _travelService.GetFriendsByTravellerIdAsync(travellerId));
    }
}
