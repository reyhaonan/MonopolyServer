using System.Security.Claims;
using System.Text.Json;
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

        group.MapPost("/discord", async (AuthRequest req, HttpResponse response, AuthService authService) =>
        {
            var discordTokenResponse = await authService.GetDiscordAccessToken(req.code);

            var discordResponse = await authService.GetDiscordAccountInfo($"{discordTokenResponse.token_type} {discordTokenResponse.access_token}");

            var user = await authService.GetOrStoreData(ProviderName.Discord, discordResponse.id, discordResponse.global_name);

            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(60);
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(30);

            var accessToken = Helpers.SetAccessTokenCookies(response, authService, user.Id.ToString(), accessTokenExpiry);
            var refreshToken = Helpers.SetRefreshTokenCookie(response, authService, user.Id.ToString(), refreshTokenExpiry);

            return TypedResults.Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
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

        group.MapPost("/guest", async (string username, HttpResponse response, AuthService authService) =>
        {
            var guestId = new Guid();

            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(60);
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(30);

            
            var accessToken = Helpers.SetAccessTokenCookies(response, authService, guestId.ToString(), accessTokenExpiry);
            var refreshToken = Helpers.SetRefreshTokenCookie(response, authService, guestId.ToString(), refreshTokenExpiry);

            return TypedResults.Ok(new
            {
                GuestId = guestId,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        });

        group.MapGet("/me", [Authorize] async (ClaimsPrincipal user) =>
        {
            var claim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid) ?? throw new InvalidDataException("No SID in the jwt(?)");

            return Results.Ok(claim.Value);
        });
        group.MapPost("/refresh", [Authorize(AuthenticationSchemes = "RefreshTokenScheme")] async (ClaimsPrincipal user, HttpResponse response, AuthService authService) =>
        {
            if (user.Claims == null) throw new InvalidOperationException("Why");
            var claim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid) ?? throw new InvalidDataException("No SID in the jwt(?)");

            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(60);
            var accessToken = Helpers.SetAccessTokenCookies(response, authService, claim.Value, accessTokenExpiry);

            return Results.Ok(accessToken);
        });
        group.MapPost("/logout", [Authorize(AuthenticationSchemes = "RefreshTokenScheme")][Authorize] async (HttpResponse response) =>
        {
            response.Cookies.Delete("XSRF-TOKEN");
            response.Cookies.Delete("AccessToken");
            response.Cookies.Delete("RefreshToken");

            return Results.Ok();
        });


    }
    
    

}
record AuthRequest(string code);