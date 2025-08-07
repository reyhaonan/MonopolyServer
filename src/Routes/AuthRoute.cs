using System.Text.Json;
using MonopolyServer.Services.Auth;

namespace MonopolyServer.Routes;

public class AuthRoute
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    public AuthRoute(IHttpClientFactory httpClientFactory, AuthService authService)
    {
        _httpClient = httpClientFactory.CreateClient();
        _authService = authService;
    }
    public void Map(WebApplication app)
    {

        app.MapPost("/oauth2/discord", async (AuthRequest req) =>
        {
            var discordTokenResponse = await _authService.GetDiscordAccessToken(req.code);

            var discordResponse = await _authService.GetDiscordAccountInfo($"{discordTokenResponse.token_type} {discordTokenResponse.access_token}");
            
            return discordResponse;
        });

        app.MapPost("/auth/login", (Guid user, HttpResponse response) =>
        {
            var (accessToken, csrfToken) = _authService.GenerateToken(user.ToString());

            response.Cookies.Delete("XSRF-TOKEN");
            response.Cookies.Append("XSRF-TOKEN", csrfToken, new CookieOptions
            {
                Expires = DateTime.UtcNow.AddMinutes(90),
                SameSite = SameSiteMode.None,
                Secure = true
            });
            response.Cookies.Delete("AccessToken");
            response.Cookies.Append("AccessToken", accessToken, new CookieOptions
            {
                Expires = DateTime.UtcNow.AddMinutes(90),
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Secure = true
            });
            return accessToken;
        });

    }
    
    

}
record AuthRequest(string code);