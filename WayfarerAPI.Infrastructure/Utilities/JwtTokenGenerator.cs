using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WayfarerAPI.Application.Interfaces.Utilities;
using WayfarerAPI.Application.Models.Common;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Infrastructure.Utilities;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenGenerator(JwtSettings jwtSettings)
    {
        _jwtSettings = jwtSettings;
    }

    public (string token, DateTime expiresAtUtc) Generate(Traveller traveller)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, traveller.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, traveller.Name),
            new(ClaimTypes.NameIdentifier, traveller.Id.ToString()),
            new(ClaimTypes.Name, traveller.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var encodedToken = new JwtSecurityTokenHandler().WriteToken(token);
        return (encodedToken, expiresAtUtc);
    }
}
