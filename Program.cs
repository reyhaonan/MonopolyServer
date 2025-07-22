using MonopolyServer.GameHubs;
using MonopolyServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.UseCors("CorsPolicy");

app.MapHub<GameHubs>("/gameHubs");

app.Run();
