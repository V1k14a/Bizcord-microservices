using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Bizcord.ApiGateway.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace Bizcord.ApiGateway.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddJwtTokenService(this IServiceCollection services, IConfiguration configuration)
    {
        var authenticationProviderKey = configuration["AuthenticationProviderKey"];

        if(authenticationProviderKey == null)
        {
            throw new ArgumentNullException($"AuthenticationProviderKey is required");
        }

        services.AddSingleton<JwtTokenService>(new JwtTokenService(authenticationProviderKey));
        return services;
    }
    public static IServiceCollection AddApiGatewayServices(this IServiceCollection services, IConfiguration configuration)
    {
        var authenticationProviderKey = configuration["AuthenticationProviderKey"];

        if(authenticationProviderKey == null)
        {
            throw new ArgumentNullException($"AuthenticationProviderKey is required");
        }
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        services.AddAuthentication("Bearer").AddJwtBearer("Bearer", options =>
        {
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authenticationProviderKey))
            };
        });
        
        services.AddOcelot(configuration);
        return services;
    }
    
    public static WebApplication UseApiGateway(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseOcelot().Wait();
        return app;
    }
}