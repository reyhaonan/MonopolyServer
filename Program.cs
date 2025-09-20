using MonopolyServer.GameHubs;
using Microsoft.AspNetCore.Authorization;
using MonopolyServer.Services;
using MonopolyServer.Routes;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MonopolyServer.Services.Auth;
using MonopolyServer.Database;
using MonopolyServer.Repositories;
using MonopolyServer.Utils;
using MonopolyServer.Middleware;
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureServices(builder);

        var app = builder.Build();

        ConfigureMiddleware(app);

        app.Run();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        // SignalR
        builder.Services.AddSignalR(options =>
        {
            options.MaximumReceiveMessageSize = 102400; // 100 KB
        });

        // Database
        builder.Services.AddDbContext<MonopolyDbContext>();

        // Dependencies
        builder.Services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
        builder.Services.AddSingleton<GameManager>();
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IUserOAuthRepository, UserOAuthRepository>();
        builder.Services.AddHostedService<KafkaSignalRNotifierService>();

        // CORS
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string>() 
                             ?? throw new Exception("AllowedOrigins is not declared");
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins.Split(", "))
                      .WithHeaders(["XSRF-TOKEN"])
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        // Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Todo API",
                Description = "Keep track of your tasks",
                Version = "v1"
            });

            var jwtSecurityScheme = new OpenApiSecurityScheme
            {
                BearerFormat = "JWT",
                Name = "Authorization",
                In = ParameterLocation.Cookie,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                Reference = new OpenApiReference
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme
                }
            };

            c.AddSecurityDefinition("Bearer", jwtSecurityScheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { jwtSecurityScheme, Array.Empty<string>() }
            });
        });

        // Authentication
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                Helpers.ConfigureJwtBearer(options, builder.Configuration);
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["AccessToken"];
                        return Task.CompletedTask;
                    }
                };
            })
            .AddJwtBearer("RefreshTokenScheme", options =>
            {
                Helpers.ConfigureJwtBearer(options, builder.Configuration);
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["RefreshToken"];
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization();

        // HttpClient
        builder.Services.AddHttpClient();

        // Sentry
        builder.WebHost.UseSentry(o =>
        {
            o.Dsn = builder.Configuration.GetSection("Sentry").GetValue<string>("Dsn");
            o.Debug = true; // enable SDK debug logs
        });
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        app.UseRouting();
        app.UseCors("CorsPolicy");
        app.UseAuthentication();

        var gameHubsUrl = "/gameHubs";
        AuthMiddleware.Use(app, gameHubsUrl);
        app.MapHub<GameHubs>(gameHubsUrl);

        app.UseAuthorization();

        AuthRoute.Map(app);
        GameRoute.Map(app);

        // Root redirect
        app.MapGet("/", context =>
        {
            context.Response.Redirect("/swagger");
            return Task.CompletedTask;
        });
        app.MapGet("/ping", () => "pong");

        // Swagger (only in development)
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo API V1");
            });
        }

        // Test message to Sentry
        SentrySdk.CaptureMessage("Hello Sentry");
    }
}