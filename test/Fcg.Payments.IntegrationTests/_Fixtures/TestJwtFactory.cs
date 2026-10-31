using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fcg.Contracts.Security;
using Microsoft.IdentityModel.Tokens;

namespace Fcg.Payments.IntegrationTests._Fixtures;

/// <summary>Emite tokens com a mesma chave/issuer/audience da UsersAPI (valores de desenvolvimento).</summary>
public static class TestJwtFactory
{
    public const string Key = "fcg-dev-only-signing-key-change-me-0123456789";
    public const string Issuer = "fcg-users-api";
    public const string Audience = "fcg";

    public static string Create(Guid userId, string role = FcgRoles.User, string name = "Teste", string email = "teste@fcg.dev")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims:
            [
                new Claim(FcgClaimTypes.UserId, userId.ToString()),
                new Claim(FcgClaimTypes.Email, email),
                new Claim(FcgClaimTypes.Name, name),
                new Claim(FcgClaimTypes.Role, role)
            ],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
