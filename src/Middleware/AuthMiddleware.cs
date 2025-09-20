using Microsoft.AspNetCore.Authorization;

namespace MonopolyServer.Middleware;
public static class AuthMiddleware {

    public static void Use(WebApplication app, string WSUrl)
    {
        app.Use((context, next) => DoubleSubmitCookieMiddleware(context, next, WSUrl));
    }
    private static Task DoubleSubmitCookieMiddleware(HttpContext context, RequestDelegate next, string WSUrl)
    {
        if (context.Request.Method == HttpMethods.Options)
        {
            return next(context);
        }
        if (context.WebSockets.IsWebSocketRequest)
        {
            return next(context);
        }
        if (context.Request.Path.StartsWithSegments(WSUrl))
        {
            return next(context);
        }

        var endpoint = context.GetEndpoint();

        var scheme = endpoint?.Metadata.GetMetadata<AuthorizeAttribute>()?.AuthenticationSchemes;
        var typeId = endpoint?.Metadata.GetMetadata<AuthorizeAttribute>()?.TypeId;

        //  XSRF-TOKEN Checks if authenticated and auth scheme used is default
        if (scheme != "RefreshTokenScheme" && typeId != null && context.User.Identity != null && context.User.Identity.IsAuthenticated)
        {
            if (context.Request.Cookies.TryGetValue("XSRF-TOKEN", out var jwtXsrfToken))
            {
                string? headerXsrfToken = context.Request.Headers["XSRF-TOKEN"].FirstOrDefault();

                if (string.IsNullOrEmpty(headerXsrfToken) || headerXsrfToken != jwtXsrfToken)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.WriteAsync("Invalid XSRF token");
                    return Task.CompletedTask;
                }
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.WriteAsync("Missing XSRF token");
                return Task.CompletedTask;
            }
        }
        return next(context);
    }
}