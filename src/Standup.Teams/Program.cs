using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Standup.Teams.Bots;
using Standup.Teams.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Bot Framework authentication
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

// Add Bot Adapter with error handling
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

// Add Bot services
builder.Services.AddSingleton<ICardService, CardService>();
builder.Services.AddHttpClient<IStandupApiService, StandupApiService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["StandupApi:BaseUrl"] ?? "https://localhost:7001");
});

// Add the bot
builder.Services.AddTransient<IBot, StandupBot>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
