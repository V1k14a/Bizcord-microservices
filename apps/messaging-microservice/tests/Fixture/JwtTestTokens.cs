using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MessagingMicroservice.Tests.Fixture;

/// <summary>
/// JWTs for integration tests; must match <c>UseSetting("Jwt:SigningKey", ...)</c> in <see cref="MessagingWebApplicationFactory"/>.
/// </summary>
public static class JwtTestTokens
{
    public const string SigningKey = "UnitTest_Signing_Key_Must_Be_32chars!!";

    public static string CreateToken(string role, string sub = "integration-user")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, sub),
            new("role", role)
        };
        var token = new JwtSecurityToken(signingCredentials: creds, claims: claims);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string User() => CreateToken("User");

    public static string Service() => CreateToken("Service");
}
