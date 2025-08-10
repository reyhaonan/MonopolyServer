using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MonopolyServer.Database;
using MonopolyServer.Database.Entities;
using MonopolyServer.Database.Enums;
using MonopolyServer.Repositories;

namespace MonopolyServer.Services.Auth;

public class AuthService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly IUserRepository _userRepository;
    private readonly IUserOAuthRepository _userOAuthRepository;

    public AuthService(IHttpClientFactory httpClientFactory, IConfiguration config, IUserRepository userRepository, IUserOAuthRepository userOAuthRepository)
    {
        _config = config;

        _httpClient = httpClientFactory.CreateClient();

        _userRepository = userRepository;
        _userOAuthRepository = userOAuthRepository;

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

        return JsonSerializer.Deserialize<DiscordTokenResponse>(tokenResponseData) ?? throw new Exception("its Empty");
    }

    public async Task<DiscordAccountResponse> GetDiscordAccountInfo(string authorizationHeader)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");

        request.Headers.Add("Authorization", authorizationHeader);

        var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<DiscordAccountResponse>(responseData) ?? throw new Exception("its Empty"); ;
    }

    public (string accessToken, string refreshToken) GenerateAccessAndRefreshToken(Guid userId, HttpResponse response)
    {
        var xsrfToken = GenerateXsrfToken();

        var accessTokenExpiry = DateTime.UtcNow.AddMinutes(60);
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(30);

        var accessToken = GenerateJWT(userId.ToString(), xsrfToken, accessTokenExpiry);
        var refreshToken = GenerateJWT(userId.ToString(), xsrfToken, refreshTokenExpiry);

        var accessTokenCookieOptions = new CookieOptions
        {
            Expires = accessTokenExpiry,
            SameSite = SameSiteMode.None,
            Secure = true
        };

        var refreshTokenCookieOptions = new CookieOptions
        {
            Expires = refreshTokenExpiry,
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            Secure = true
        };

        response.Cookies.Delete("XSRF-TOKEN");
        response.Cookies.Append("XSRF-TOKEN", xsrfToken, accessTokenCookieOptions);

        response.Cookies.Delete("AccessToken");
        response.Cookies.Append("AccessToken", accessToken, accessTokenCookieOptions);
        response.Cookies.Delete("RefreshToken");
        response.Cookies.Append("RefreshToken", accessToken, refreshTokenCookieOptions);

        return (accessToken, refreshToken);
    }

    public string GenerateJWT(string user, string xsrfToken, DateTime expires)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new Dictionary<string, object>
        {
            [ClaimTypes.Sid] = user,
            ["xsrf_token"] = xsrfToken
        };

        var descriptor = new SecurityTokenDescriptor
        {

            Issuer = _config["JWT:Issuer"],
            Audience = _config["JWT:Audience"],
            Claims = claims,
            Expires = expires,
            SigningCredentials = credentials,
        };

        var handler = new JsonWebTokenHandler();
        handler.SetDefaultTimesOnTokenCreation = false;

        return handler.CreateToken(descriptor);
    }

    public string GenerateXsrfToken(int length = 32)
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

    public async Task<User> GetOrStoreData(ProviderName providerName, string id)
    {
        var existingOAuth = await _userOAuthRepository.GetByProviderNameAndId(providerName, id);
        if (existingOAuth != null) return existingOAuth.User;

        Console.WriteLine($"KKKKKK Creating new User");
        var newOAuth = await _userOAuthRepository.Create(new UserOAuth
        {
            OAuthID = id,
            ProviderName = providerName,
        });

        var newUser = await _userRepository.Create(new User
        {
            Username = new Guid().ToString()
        });

        await _userOAuthRepository.UpdateUserId(newOAuth.Id, newUser.Id);

        return newUser;
    }
}