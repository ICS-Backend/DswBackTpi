using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Dsw2025Tpi.Tests.Integration.Infrastructure;

/// <summary>
/// Genera tokens JWT firmados con la misma clave, issuer y audience que valida la API en los tests.
/// Así no hace falta crear usuarios en Identity ni llamar a /api/auth/login.
/// </summary>
public static class JwtTokenHelper
{
    public static string CreateToken(string role, string userName = "test-user")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(CustomWebApplicationFactory.JwtKey));
        var token = new JwtSecurityToken(
            issuer: CustomWebApplicationFactory.JwtIssuer,
            audience: CustomWebApplicationFactory.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Crea un HttpClient con el header Authorization: Bearer ya cargado.</summary>
    public static HttpClient CreateClientWithRole(this CustomWebApplicationFactory factory, string role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(role));
        return client;
    }
}
