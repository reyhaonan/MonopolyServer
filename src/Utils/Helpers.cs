using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MonopolyServer.Services.Auth;

namespace MonopolyServer.Utils;

public static class Helpers
{
    public static (string accessToken, string refreshToken) SetAuthCookies(HttpResponse response, AuthService authService, string userId, DateTime accessTokenExpiry, DateTime refreshTokenExpiry)
    {
        var xsrfToken = authService.GenerateXsrfToken();

        var accessToken = authService.GenerateJWT(userId, xsrfToken, accessTokenExpiry);
        var refreshToken = authService.GenerateJWT(userId, xsrfToken, refreshTokenExpiry);

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
        response.Cookies.Append("RefreshToken", refreshToken, refreshTokenCookieOptions);

        return (accessToken, refreshToken);
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
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = (context) =>
            {
                if (context.Request.Method == HttpMethods.Post ||
                    context.Request.Method == HttpMethods.Put ||
                    context.Request.Method == HttpMethods.Delete)
                {
                    string? headerXsrfToken = context.Request.Headers["XSRF-TOKEN"].FirstOrDefault();
                    string? jwtXsrfToken = context.Principal?.FindFirst("xsrf_token")?.Value;

                    if (string.IsNullOrEmpty(headerXsrfToken) || string.IsNullOrEmpty(jwtXsrfToken) || headerXsrfToken != jwtXsrfToken)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Fail("CSRF token validation failed.");
                        return Task.CompletedTask;
                    }
                }
                return Task.CompletedTask;
            }
        };
    }
}