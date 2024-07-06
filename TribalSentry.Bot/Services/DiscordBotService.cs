using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TribalSentry.Bot.Services;

public class DiscordBotService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly CommandHandler _commandHandler;
    private readonly ILogger<DiscordBotService> _logger;
    private readonly MonitorService _monitorService;
    private readonly IConfiguration _configuration;

    public DiscordBotService(
        DiscordSocketClient client,
        CommandHandler commandHandler,
        ILogger<DiscordBotService> logger,
        MonitorService monitorService,
        IConfiguration configuration)
    {
        _client = client;
        _commandHandler = commandHandler;
        _logger = logger;
        _monitorService = monitorService;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;

        string botToken = _configuration["Discord:BotToken"];
        if (string.IsNullOrEmpty(botToken))
        {
            _logger.LogError("Bot token not found in configuration.");
            return;
        }

        await _client.LoginAsync(TokenType.Bot, botToken);
        await _client.StartAsync();
        await _commandHandler.InitializeAsync();
        await _monitorService.StartMonitoringAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
    }

    private Task LogAsync(LogMessage log)
    {
        _logger.LogInformation(log.ToString());
        return Task.CompletedTask;
    }

    private async Task ReadyAsync()
    {
        try
        {
            List<ApplicationCommandProperties> applicationCommandProperties = new();

            var addMonitorCommand = new SlashCommandBuilder()
                .WithName("addmonitor")
                .WithDescription("Add a new monitor for barbarian villages")
                .AddOption("market", ApplicationCommandOptionType.String, "The Tribal Wars market (e.g., tribalwars.co.uk)", isRequired: true)
                .AddOption("worldname", ApplicationCommandOptionType.String, "The world name (e.g., uk75)", isRequired: true)
                .AddOption("minpoints", ApplicationCommandOptionType.Integer, "Minimum points for monitored villages", isRequired: true)
                .AddOption("continent", ApplicationCommandOptionType.String, "The continent to monitor (e.g., K55)", isRequired: true)
                .Build();

            var updateMonitorCommand = new SlashCommandBuilder()
                .WithName("updatemonitor")
                .WithDescription("Update an existing monitor")
                .AddOption("id", ApplicationCommandOptionType.String, "The ID of the monitor to update", isRequired: true)
                .AddOption("market", ApplicationCommandOptionType.String, "The Tribal Wars market (e.g., tribalwars.co.uk)", isRequired: true)
                .AddOption("worldname", ApplicationCommandOptionType.String, "The world name (e.g., uk75)", isRequired: true)
                .AddOption("minpoints", ApplicationCommandOptionType.Integer, "Minimum points for monitored villages", isRequired: true)
                .AddOption("continent", ApplicationCommandOptionType.String, "The continent to monitor (e.g., K55)", isRequired: true)
                .Build();

            var removeMonitorCommand = new SlashCommandBuilder()
                .WithName("removemonitor")
                .WithDescription("Remove an existing monitor")
                .AddOption("id", ApplicationCommandOptionType.String, "The ID of the monitor to remove", isRequired: true)
                .Build();

            var viewMonitorsCommand = new SlashCommandBuilder()
                    .WithName("viewmonitors")
                    .WithDescription("View all active monitors")
                    .Build();

            applicationCommandProperties.Add(addMonitorCommand);
            applicationCommandProperties.Add(viewMonitorsCommand);
            applicationCommandProperties.Add(updateMonitorCommand);
            applicationCommandProperties.Add(removeMonitorCommand);

            await _client.BulkOverwriteGlobalApplicationCommandsAsync(applicationCommandProperties.ToArray());
            _logger.LogInformation("Slash commands registered successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while registering slash commands.");
        }
    }
}