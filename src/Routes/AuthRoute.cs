using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MonopolyServer.Services;

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
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                // ima invalidate this later
                { "client_id", "1402626488079224862" },
                { "client_secret", "GPM0FARhRvHDeVfEI3QUuM1xQTNH_VJG" },
                { "code", req.code },
                { "grant_type", "authorization_code" },
                { "redirect_uri", "http://localhost:5173/oauth" },
                { "scope", "identify" }
            });
            var response = await _httpClient.PostAsync("https://discord.com/api/oauth2/token", content);

            response.EnsureSuccessStatusCode();
            var tokenResponseData = await response.Content.ReadAsStringAsync();
        
            return tokenResponseData;
        });

        app.MapPost("/auth/login", (Guid user, HttpResponse response) =>
        {
            var (accessToken, csrfToken) = _authService.GenerateToken(user);

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