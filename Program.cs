using MonopolyServer.GameHubs;
using MonopolyServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure SignalR with Newtonsoft.Json to handle polymorphic types
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 102400; // 100 KB
});

var AllowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
builder.Services.AddSingleton<GameService>();

builder.Services.AddHostedService<KafkaSignalRNotifierService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(AllowedOrigins ?? []).AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseHsts();


app.UseCors("CorsPolicy");

app.MapHub<GameHubs>("/gameHubs");

app.Run();