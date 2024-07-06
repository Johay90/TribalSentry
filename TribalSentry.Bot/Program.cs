using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using TribalSentry.Bot.Services;
using Microsoft.Extensions.Configuration;

namespace TribalSentry.Bot;

public class Program
{
    public static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<DiscordBotService>();
                services.AddSingleton<DiscordSocketClient>();
                services.AddSingleton<CommandService>();
                services.AddSingleton<CommandHandler>();
                services.AddSingleton<TribalWarsApiService>();
                services.AddSingleton<MonitorService>();
            })
            .RunConsoleAsync();
    }
}