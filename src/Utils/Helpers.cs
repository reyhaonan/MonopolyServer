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
}