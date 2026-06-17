using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DocFlow.Api.Auth;

public interface IJwtTokenService
{
    LoginResponse CreateToken(string userName, string role);
}

public sealed class JwtTokenService : IJwtTokenService
{
    public const string Issuer = "DocFlow";
    public const string Audience = "DocFlow.Api";
    public const string SigningKey = "DocFlowLocalDevelopmentKeyThatIsLongEnoughForHmacSha256";

    public LoginResponse CreateToken(string userName, string role)
    {
        var expiresAtUtc = DateTime.UtcNow.AddHours(2);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userName),
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginResponse(accessToken, "Bearer", expiresAtUtc, userName, role);
    }
}
