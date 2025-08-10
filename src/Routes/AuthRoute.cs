using System.Text.Json;
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

            var user = await authService.GetOrStoreData(ProviderName.Discord, discordResponse.id);

            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(60);
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(30);

            var (accessToken, refreshToken) = Helpers.SetAuthCookies(response, authService, user.Id.ToString(), accessTokenExpiry, refreshTokenExpiry);

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
            // return Results.Ok(authService.GenerateAccessAndRefreshToken(guestId, response));
        });

        group.MapPost("/guest", async (string username, HttpResponse response, AuthService authService) =>
        {
            var guestId = new Guid();
            
            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(60);
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(30);

            var (accessToken, refreshToken) = Helpers.SetAuthCookies(response, authService, guestId.ToString(), accessTokenExpiry, refreshTokenExpiry);

            return Results.Ok(new
            {
                accessToken,
                refreshToken
            });
        });
        


    }
    
    

}
record AuthRequest(string code);