using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseModularMonolith.Api.Composition;

public static class DevAuthEndpoints
{
    public static void MapDevAuthEndpoints(this IEndpointRouteBuilder endpoints, IConfiguration configuration)
    {
        endpoints.MapPost("/api/v1/auth/login", () =>
        {
            var jwtSettings = configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["SigningKey"]!);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "00000000-0000-0000-0000-000000000001"),
                new Claim(JwtRegisteredClaimNames.Email, "dev@example.com"),
                new Claim(JwtRegisteredClaimNames.Name, "Developer User"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["AccessTokenLifetimeMinutes"] ?? "60")),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            );

            return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        })
        .WithTags("Dev Auth")
        .AllowAnonymous();
    }
}
