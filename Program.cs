using MonopolyServer.GameHubs;
using Microsoft.AspNetCore.Authorization;
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
using Confluent.Kafka;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Security.Claims;
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
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["AccessToken"];
                return Task.CompletedTask;
            },
        }; 
    })
    // Refresh Token configuration
    .AddJwtBearer("RefreshTokenScheme", options =>
    {
        Helpers.ConfigureJwtBearer(options, builder.Configuration);
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["RefreshToken"];;
                return Task.CompletedTask;
            }
        };
    });



builder.Services.AddAuthorization();

var app = builder.Build();

app.UseRouting();

app.UseCors("CorsPolicy");
app.UseAuthentication();

app.Use((context, next) => {

    var endpoint = context.GetEndpoint();

    var scheme = endpoint?.Metadata.GetMetadata<AuthorizeAttribute>()?.AuthenticationSchemes;
    var typeId = endpoint?.Metadata.GetMetadata<AuthorizeAttribute>()?.TypeId;
    //  XSRF-TOKEN Checks if authenticated and auth scheme used is default
    if (scheme != "RefreshTokenScheme" && typeId != null && context.User.Identity.IsAuthenticated)
    {
        string? headerXsrfToken = context.Request.Headers["XSRF-TOKEN"].FirstOrDefault();
        string? jwtXsrfToken = context.Request.Cookies.FirstOrDefault(e => e.Key == "XSRF-TOKEN").Value;

        if (string.IsNullOrEmpty(headerXsrfToken) || string.IsNullOrEmpty(jwtXsrfToken) || headerXsrfToken != jwtXsrfToken)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.WriteAsync("Invalid XSRF token");
            return Task.CompletedTask;
        }
    }
    return next(context);
});

app.MapHub<GameHubs>("/gameHubs");
AuthRoute.Map(app);
GameRoute.Map(app);

app.UseAuthorization();
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});




if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo API V1");
    });
}

app.Run();
