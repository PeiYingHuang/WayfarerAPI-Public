using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Reflection;
using System.Security.Claims;
using WayfarerAPI.Application.Models.Common;

namespace WayfarerAPI.Api.Controllers;

[ApiController]
public class ApiControllerBase : Controller
{
    protected bool _isSuccess = true;
    protected string _message = string.Empty;

    /// <summary>
    /// 從 JWT Token 中取得當前用戶的 GUID，解析失敗時拋出 UnauthorizedAccessException
    /// </summary>
    protected Guid GetCurrentUserId()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("用戶未登入或 Token 無效");
        }

        if (!Guid.TryParse(userId, out var guid))
        {
            throw new UnauthorizedAccessException("無效的用戶身份格式");
        }

        return guid;
    }

    protected IActionResult ApiResult()
    {
        var result = new Result<object>
        {
            IsSuccess = _isSuccess,
            Message = _message,
            Data = null
        };

        return new ObjectResult(result);
    }

    protected IActionResult ApiResult(bool isSuccess, string message = "")
    {
        _isSuccess = isSuccess;
        _message = message;
        return ApiResult();
    }

    protected IActionResult ApiResult<T>(T? data, string message, bool isSuccess)
    {
        _isSuccess = isSuccess;
        _message = message;

        var result = new Result<T>
        {
            IsSuccess = _isSuccess,
            Message = _message,
            Data = data
        };

        return new ObjectResult(result);
    }

    protected IActionResult ApiResult<T>(T? data)
    {
        if (data == null)
        {
            _isSuccess = false;
            if (string.IsNullOrEmpty(_message))
            {
                var type = typeof(T);
                var name = type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? type.Name;
                _message = $"{name} is not found.";
            }
        }
        var result = new Result<T>
        {
            IsSuccess = _isSuccess,
            Message = _message,
            Data = data
        };

        return new ObjectResult(result);
    }
}
