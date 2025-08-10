using MonopolyServer.GameHubs;
using MonopolyServer.Services;
using MonopolyServer.Routes;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MonopolyServer.Services.Auth;
using MonopolyServer.Database;
using MonopolyServer.Repositories;
using MonopolyServer.Utils;
var builder = WebApplication.CreateBuilder(args);

// Configure SignalR with Newtonsoft.Json to handle polymorphic types
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 102400; // 100 KB
});
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<MonopolyDbContext>();

// Register event publisher
builder.Services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
builder.Services.AddSingleton<GameService>();

builder.Services.AddScoped<AuthService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserOAuthRepository, UserOAuthRepository>();

builder.Services.AddHostedService<KafkaSignalRNotifierService>();

var AllowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(AllowedOrigins ?? []).AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddHttpClient();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Todo API", Description = "Keep track of your tasks", Version = "v1" });
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
        {jwtSecurityScheme, Array.Empty<string>()}
    });
});

// Access Token configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        Helpers.ConfigureJwtBearer(options, builder.Configuration);
        options.Events.OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["AccessToken"];
            return Task.CompletedTask;
        };
        options.Events.OnTokenValidated = (context) =>
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
            };
    })
    // Refresh Token configuration
    .AddJwtBearer("RefreshTokenScheme", options =>
    {
        Helpers.ConfigureJwtBearer(options, builder.Configuration);
        options.Events.OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["RefreshToken"];
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseRouting();

app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<GameHubs>("/gameHubs");
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});


AuthRoute.Map(app);
GameRoute.Map(app);


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo API V1");
    });
}

app.Run();
