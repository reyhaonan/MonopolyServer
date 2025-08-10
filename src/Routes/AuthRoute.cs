using System.Text.Json;
using MonopolyServer.Database.Enums;
using MonopolyServer.DTO;
using MonopolyServer.Services.Auth;

namespace MonopolyServer.Routes;

public static class AuthRoute
{
    public static void Map(WebApplication app)
    {

        app.MapPost("/oauth2/discord", async (AuthRequest req, HttpResponse response, AuthService authService) =>
        {
            // TODO: Generate token
            var discordTokenResponse = await authService.GetDiscordAccessToken(req.code);

            var discordResponse = await authService.GetDiscordAccountInfo($"{discordTokenResponse.token_type} {discordTokenResponse.access_token}");

            var user = await authService.GetOrStoreData(ProviderName.Discord,discordResponse.id);

            return TypedResults.Ok(new UserDTO
            {
                Id = user.Id,
                Username = user.Username,
                OAuth = user.OAuth.Select(o => new UserOAuthDTO
                {
                    Id = o.Id,
                    OAuthID = o.OAuthID,
                    ProviderName = o.ProviderName.ToString()
                }).ToList()
            });
            // return Results.Ok(authService.GenerateAccessAndRefreshToken(guestId, response));
        });

        app.MapPost("/auth/guest", async (string username, HttpResponse response, AuthService authService) =>
        {
            var guestId = new Guid();

            var (accessToken, refreshToken) = authService.GenerateAccessAndRefreshToken(guestId, response);

            return Results.Ok(new
            {
                accessToken,
                refreshToken
            });
        });
        


    }
    
    

}
record AuthRequest(string code);