using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MonopolyServer.Services.Auth;

namespace MonopolyServer.Utils;

public static class Helpers
{
    public static string SetAccessTokenCookies(HttpResponse response, AuthService authService, string userId, DateTime accessTokenExpiry)
    {
        var xsrfToken = authService.GenerateXsrfToken();

        var accessTokenClaims = new Dictionary<string, object>
        {
            [ClaimTypes.Sid] = userId,
        };

        var accessToken = authService.GenerateJWT(accessTokenClaims, accessTokenExpiry);

        var accessTokenCookieOptions = new CookieOptions
        {
            Expires = accessTokenExpiry,
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            Secure = true
        };

        response.Cookies.Delete("XSRF-TOKEN");
        response.Cookies.Append("XSRF-TOKEN", xsrfToken, new CookieOptions
        {
            Expires = accessTokenExpiry,
            SameSite = SameSiteMode.None,
            Secure = true
        });

        response.Cookies.Delete("AccessToken");
        response.Cookies.Append("AccessToken", accessToken, accessTokenCookieOptions);

        return accessToken;
    }

    public static string SetRefreshTokenCookie(HttpResponse response, AuthService authService, string userId, DateTime refreshTokenExpiry)
    {
        var refreshTokenClaims = new Dictionary<string, object>
        {
            [ClaimTypes.Sid] = userId
        };
        var refreshToken = authService.GenerateJWT(refreshTokenClaims, refreshTokenExpiry);

        var refreshTokenCookieOptions = new CookieOptions
        {
            Expires = refreshTokenExpiry,
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            Secure = true
        };

        response.Cookies.Delete("RefreshToken");
        response.Cookies.Append("RefreshToken", refreshToken, refreshTokenCookieOptions);

        return refreshToken;
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Key"]))
        };
    }
}