namespace WayfarerAPI.Application.DTOs;

// Response DTOs

/// <summary>
/// Auth Response DTO - 認證回應資料
/// </summary>
public sealed class AuthResponseDto
{
    public Guid TravellerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
}

// Request DTOs

/// <summary>
/// Login Request DTO - 登入請求
/// </summary>
public sealed class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Register Request DTO - 註冊請求
/// </summary>
public sealed class RegisterRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Refresh Token Request DTO - 刷新令牌請求
/// </summary>
public sealed class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Change Password Request DTO - 變更密碼請求
/// </summary>
public sealed class ChangePasswordRequestDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Update Profile Request DTO - 更新個人資料請求
/// </summary>
public sealed class UpdateProfileRequestDto
{
    public string DisplayName { get; set; } = string.Empty;
}
