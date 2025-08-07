using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MonopolyServer.Services.Auth;

public class AuthService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public AuthService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _config = config;
        
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<DiscordTokenResponse> GetDiscordAccessToken(string code)
    {
        
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                // ima invalidate this later
                { "client_id", _config["OAuth2:Discord:ClientId"] },
                { "client_secret",  _config["OAuth2:Discord:ClientSecret"]  },
                { "code", code },
                { "grant_type", "authorization_code" },
                { "redirect_uri", "http://localhost:5173/oauth2" },
                { "scope", "identify" }
            });
            var response = await _httpClient.PostAsync("https://discord.com/api/oauth2/token", content);
            response.EnsureSuccessStatusCode();
            var tokenResponseData = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"BABABOOOEY2 {tokenResponseData}");
            return JsonSerializer.Deserialize<DiscordTokenResponse>(tokenResponseData) ?? throw new Exception("its Empty");
    }

    public async Task<DiscordAccountResponse> GetDiscordAccountInfo(string authorizationHeader)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");

        request.Headers.Add("Authorization", authorizationHeader);

        var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<DiscordAccountResponse>(responseData) ?? throw new Exception("its Empty");;
    }

    public (string, string) GenerateToken(string user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var csrfToken = GenerateCsrfToken();

        var claims = new Dictionary<string, object>
        {
            [ClaimTypes.Sid] = user,
            ["xsrf_token"] = csrfToken
        };

        var descriptor = new SecurityTokenDescriptor
        {

            Issuer = _config["JWT:Issuer"],
            Audience = _config["JWT:Audience"],
            Claims = claims,
            Expires = DateTime.UtcNow.AddMinutes(120),
            SigningCredentials = credentials,
        };

        var handler = new JsonWebTokenHandler();
        handler.SetDefaultTimesOnTokenCreation = false;

        return (handler.CreateToken(descriptor), csrfToken);
    }

    public static string GenerateCsrfToken(int length = 32)
    {
        // Create a byte array to hold the random bytes.
        byte[] tokenBytes = new byte[length];

        // Fill the array with cryptographically strong random bytes.
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }

        // Convert the byte array to a hexadecimal string.
        // This makes it safe to transmit and store as text.
        StringBuilder sb = new StringBuilder(length * 2); // Each byte becomes two hex characters
        foreach (byte b in tokenBytes)
        {
            sb.AppendFormat("{0:x2}", b);
        }

        return sb.ToString();
    }
}