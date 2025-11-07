using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Vowlt.Api.Features.Auth.Options;

namespace Vowlt.Api.Features.Auth.Services;

public class JwtTokenGenerator(
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider) : IJwtTokenGenerator
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public string GenerateAccessToken(Guid userId, string email)
    {
        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.Secret));

        var signingCredentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
              new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
              new Claim(JwtRegisteredClaimNames.Email, email),
              new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
          };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: timeProvider.GetUtcNow()
                .AddMinutes(_jwtOptions.AccessTokenExpiryMinutes)
                .UtcDateTime,
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
