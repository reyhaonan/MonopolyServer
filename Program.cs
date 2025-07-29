using MonopolyServer.GameHubs;
using MonopolyServer.Services;
using MonopolyServer.Routes;
using Microsoft.OpenApi.Models;
var builder = WebApplication.CreateBuilder(args);

// Configure SignalR with Newtonsoft.Json to handle polymorphic types
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 102400; // 100 KB
});
builder.Services.AddEndpointsApiExplorer();

var AllowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();

// Register event publisher
builder.Services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
builder.Services.AddSingleton<GameService>();
builder.Services.AddSingleton<GameRoute>();

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

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Todo API", Description = "Keep track of your tasks", Version = "v1" });
});
var app = builder.Build();

app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.UseHsts();
app.UseCors("CorsPolicy");
        using (var scope = app.Services.CreateScope())
        {
            var gameRoute = scope.ServiceProvider.GetRequiredService<GameRoute>();
                        gameRoute.Map(app);
        }


if (app.Environment.IsDevelopment())
{
     
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo API V1");
    });
}
app.MapHub<GameHubs>("/gameHubs");





app.Run();