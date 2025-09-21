using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MonopolyServer.Database.Enums;
using MonopolyServer.DTO;
using MonopolyServer.Services.Auth;
using MonopolyServer.Utils;

namespace MonopolyServer.Routes;

public static class AuthRoute
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/discord", async (AuthRequest req, HttpResponse response, AuthService authService, IConfiguration configuration) =>
        {
            var discordTokenResponse = await authService.GetDiscordAccessToken(req.code);

            var discordResponse = await authService.GetDiscordAccountInfo($"{discordTokenResponse.token_type} {discordTokenResponse.access_token}");

            var user = await authService.GetOrStoreData(ProviderName.Discord, discordResponse.id, discordResponse.global_name);

            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(60);
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(30);

            
            var cookieDomain = configuration.GetValue<string>("CookieDomain") ?? throw new Exception("CookieDomain is not set");

            var accessToken = Helpers.SetAccessTokenCookies(response, authService, user.Id.ToString(), accessTokenExpiry, cookieDomain);
            Helpers.SetRefreshTokenCookie(response, authService, user.Id.ToString(),user.Username, refreshTokenExpiry, cookieDomain);

            return TypedResults.Ok(new
            {
                AccessToken = accessToken,
                User = new UserDTO
                {
                    Id = user.Id,
                    Username = user.Username,
                    OAuth = user.OAuth.Select(o => new UserOAuthDTO
                    {
                        Id = o.Id,
                        OAuthID = o.OAuthID,
                        ProviderName = o.ProviderName.ToString()
                    }).ToList()
                }
            });
        });

        group.MapPost("/guest", (string username, HttpResponse response, AuthService authService, IConfiguration configuration) =>
        {
            var guestId = Guid.NewGuid();

            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(60);
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(30);

            var cookieDomain = configuration.GetValue<string>("CookieDomain") ?? throw new Exception("CookieDomain is not set");
            var accessToken = Helpers.SetAccessTokenCookies(response, authService, guestId.ToString(), accessTokenExpiry, cookieDomain);
            Helpers.SetRefreshTokenCookie(response, authService, guestId.ToString(), username, refreshTokenExpiry, cookieDomain);

            return TypedResults.Ok(new
            {
                AccessToken = accessToken,
                User = new UserDTO
                {
                    Id = guestId,
                    Username = username,
                },
            });
        });

        group.MapGet("/me", [Authorize] (ClaimsPrincipal user) =>
        {
            var claim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid) ?? throw new InvalidDataException("No SID in the jwt(?)");

            return Results.Ok(claim.Value);
        });
        group.MapPost("/refresh", [Authorize(AuthenticationSchemes = "RefreshTokenScheme")] (ClaimsPrincipal user, HttpResponse response, AuthService authService, IConfiguration configuration) =>
        {
            if (user.Claims == null) throw new InvalidOperationException("Why");
            var claim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid) ?? throw new InvalidDataException("No SID in the jwt(?)");

            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(60);

            
            var cookieDomain = configuration.GetValue<string>("CookieDomain") ?? throw new Exception("CookieDomain is not set");
            var accessToken = Helpers.SetAccessTokenCookies(response, authService, claim.Value, accessTokenExpiry, cookieDomain);

            return Results.Ok(new
            {
                AccessToken = accessToken,
            });
        });
        group.MapPost("/logout", [Authorize(AuthenticationSchemes = "RefreshTokenScheme")] (HttpResponse response, IConfiguration configuration) =>
        {
            var cookieDomain = configuration.GetValue<string>("CookieDomain") ?? throw new Exception("CookieDomain is not set");
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Domain = cookieDomain,
                Path="/"
            };
            response.Cookies.Delete("XSRF-TOKEN", new CookieOptions
            {
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Domain = cookieDomain,
                Path="/"
            });
            response.Cookies.Delete("AccessToken", cookieOptions);
            response.Cookies.Delete("RefreshToken", cookieOptions);

            return Results.Ok();
        });


    }
    
    

}
record AuthRequest(string code);