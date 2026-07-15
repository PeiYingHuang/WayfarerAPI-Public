using System.Security.Cryptography;
using System.Text;
using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Application.Interfaces.Service;
using WayfarerAPI.Application.Interfaces.Utilities;
using WayfarerAPI.Application.Models.Common;
using WayfarerAPI.Domain.Entities;
using WayfarerAPI.Domain.Enumerations;

namespace WayfarerAPI.Application.Services;

public sealed class AuthService : IAuthService
{
    private const string PasswordAlgo = "PBKDF2-HMACSHA256";

    private readonly ITravellerRepository _travellerRepository;
    private readonly ITravellerCredentialRepository _travellerCredentialRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly JwtSettings _jwtSettings;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        ITravellerRepository travellerRepository,
        ITravellerCredentialRepository travellerCredentialRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        JwtSettings jwtSettings,
        IUnitOfWork unitOfWork)
    {
        _travellerRepository = travellerRepository;
        _travellerCredentialRepository = travellerCredentialRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _jwtSettings = jwtSettings;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        ValidateRegisterRequest(request);

        var email = request.Email.Trim().ToLowerInvariant();
        var existingUser = await _travellerCredentialRepository.GetByEmailAsync(email);
        if (existingUser is not null)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var travellerId = Guid.CreateVersion7();
        var traveller = new Traveller
        {
            Id = travellerId,
            Name = request.Name.Trim()
        };

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _travellerRepository.InsertAsync(traveller);
            await _travellerCredentialRepository.InsertAsync(new TravellerCredential
            {
                Id = Guid.CreateVersion7(),
                TravellerId = travellerId,
                Provider = PasswordProviderEnum.Password.ToString(),
                ProviderUserId = email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                PasswordAlgo = PasswordAlgo
            });
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }

        return await BuildAuthResponseAsync(traveller);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Email and password are required.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var credential = await _travellerCredentialRepository.GetByEmailAsync(email);

        if (credential is null || !_passwordHasher.Verify(request.Password, credential.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var traveller = await _travellerRepository.GetByIdAsync(credential.TravellerId);
        if (traveller is null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return await BuildAuthResponseAsync(traveller);
    }

    public async Task<AuthResponseDto> RefreshAsync(RefreshTokenRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new ArgumentException("Refresh token is required.");
        }

        var incomingTokenHash = ComputeSha256(request.RefreshToken.Trim());
        var existingRefreshToken = await _refreshTokenRepository.GetValidByHashAsync(incomingTokenHash);
        if (existingRefreshToken is null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var user = await _travellerRepository.GetByIdAsync(existingRefreshToken.TravellerId);
        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        await _refreshTokenRepository.RevokeAsync(existingRefreshToken.Id);
        return await BuildAuthResponseAsync(user);
    }

    public async Task UpdateProfileAsync(Guid travellerId, UpdateProfileRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new ArgumentException("Display name is required.");
        }

        var traveller = await _travellerRepository.GetByIdAsync(travellerId);
        if (traveller is null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        traveller.Name = request.DisplayName.Trim();

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _travellerRepository.UpdateAsync(traveller);
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task ChangePasswordAsync(Guid travellerId, ChangePasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ArgumentException("Current password and new password are required.");
        }

        if (request.NewPassword.Length < 8)
        {
            throw new ArgumentException("New password must be at least 8 characters.");
        }

        var credential = await _travellerCredentialRepository.GetByProviderAsync(travellerId, PasswordProviderEnum.Password);
        if (credential is null || !_passwordHasher.Verify(request.CurrentPassword, credential.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid current password.");
        }

        credential.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        credential.PasswordAlgo = PasswordAlgo;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _travellerCredentialRepository.UpdateAsync(credential);
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private static void ValidateRegisterRequest(RegisterRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Email, password, and name are required.");
        }

        if (request.Password.Length < 8)
        {
            throw new ArgumentException("Password must be at least 8 characters.");
        }
    }

    private async Task<AuthResponseDto> BuildAuthResponseAsync(Traveller traveller)
    {
        var (accessToken, accessTokenExpiresAtUtc) = _jwtTokenGenerator.Generate(traveller);

        var refreshTokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiresDays);

        var refreshToken = new RefreshToken
        {
            TravellerId = traveller.Id,
            TokenHash = ComputeSha256(refreshTokenValue),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refreshTokenExpiresAtUtc
        };

        await _refreshTokenRepository.CreateAsync(refreshToken);

        return new AuthResponseDto
        {
            TravellerId = traveller.Id,
            Name = traveller.Name,
            AccessToken = accessToken,
            ExpiresAtUtc = accessTokenExpiresAtUtc,
            RefreshToken = refreshTokenValue,
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc
        };
    }

    private static string ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
