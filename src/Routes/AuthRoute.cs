using Microsoft.AspNetCore.Authorization;
using MonopolyServer.Services;

namespace MonopolyServer.Routes;

public class AuthRoute
{
    private readonly HttpClient _httpClient;
    public AuthRoute(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }
    public void Map(WebApplication app)
    {

        app.MapPost("/oauth2/discord", async (AuthRequest req) =>
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
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

    }
}
record AuthRequest(string code);