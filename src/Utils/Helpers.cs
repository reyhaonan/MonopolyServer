using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MonopolyServer.Services.Auth;

namespace MonopolyServer.Utils;

public static class Helpers
{
    private static CookieOptions BuildCookieOptions(DateTime expiry, string domain, bool httpOnly = false)
    {
        return new CookieOptions
        {
            Expires = expiry,
            HttpOnly = httpOnly,
            SameSite = SameSiteMode.Strict,
            Secure = true,
            Domain = domain,
            Path = "/"
        };
    }

    private static CookieOptions BuildDeleteOptions(string domain, bool httpOnly = false)
    {
        return new CookieOptions
        {
            Expires = DateTime.UnixEpoch, // past date = delete
            HttpOnly = httpOnly,
            SameSite = SameSiteMode.Strict,
            Secure = true,
            Domain = domain,
            Path = "/"
        };
    }

    private static void SetCookie(HttpResponse response, string name, string value, CookieOptions options)
    {
        response.Cookies.Delete(name, options); // delete with same attributes
        response.Cookies.Append(name, value, options);
    }

    public static string SetAccessTokenCookies(
        HttpResponse response,
        AuthService authService,
        string userId,
        DateTime accessTokenExpiry,
        string cookieDomain)
    {
        var xsrfToken = authService.GenerateXsrfToken();

        var accessTokenClaims = new Dictionary<string, object>
        {
            [ClaimTypes.Sid] = userId
        };

        var accessToken = authService.GenerateJWT(accessTokenClaims, accessTokenExpiry);

        // Access token (HttpOnly)
        SetCookie(response, "AccessToken", accessToken, BuildCookieOptions(accessTokenExpiry, cookieDomain, httpOnly: true));

        // XSRF token (not HttpOnly)
        SetCookie(response, "XSRF-TOKEN", xsrfToken, BuildCookieOptions(accessTokenExpiry, cookieDomain));

        return accessToken;
    }

    public static string SetRefreshTokenCookie(
        HttpResponse response,
        AuthService authService,
        string userId,
        string username,
        DateTime refreshTokenExpiry,
        string cookieDomain)
    {
        var refreshTokenClaims = new Dictionary<string, object>
        {
            [ClaimTypes.Sid] = userId
        };

        var refreshToken = authService.GenerateJWT(refreshTokenClaims, refreshTokenExpiry);

        // Refresh token (HttpOnly)
        SetCookie(response, "RefreshToken", refreshToken, BuildCookieOptions(refreshTokenExpiry, cookieDomain, httpOnly: true));

        // Username (not HttpOnly)
        SetCookie(response, "Username", username, BuildCookieOptions(refreshTokenExpiry, cookieDomain));

        return refreshToken;
    }

    public static void ClearAuthCookies(HttpResponse response, string cookieDomain)
    {
        // Delete with correct HttpOnly flags
        response.Cookies.Delete("AccessToken", BuildDeleteOptions(cookieDomain, httpOnly: true));
        response.Cookies.Delete("RefreshToken", BuildDeleteOptions(cookieDomain, httpOnly: true));

        response.Cookies.Delete("XSRF-TOKEN", BuildDeleteOptions(cookieDomain));
        response.Cookies.Delete("Username", BuildDeleteOptions(cookieDomain));
    }

    public static void ConfigureJwtBearer(JwtBearerOptions options, IConfiguration configuration)
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["JWT:Issuer"],
            ValidAudience = configuration["JWT:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    configuration.GetSection("JWT").GetValue<string>("Key") 
                    ?? throw new Exception("Missing Jwt Key")))
        };
    }
}
