using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using TribalSentry.Bot.Models;
using Monitor = TribalSentry.Bot.Models.Monitor;

namespace TribalSentry.Bot.Services
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly MonitorService _monitorService;
        private readonly ILogger<CommandHandler> _logger;

        public CommandHandler(DiscordSocketClient client, MonitorService monitorService, ILogger<CommandHandler> logger)
        {
            _client = client;
            _monitorService = monitorService;
            _logger = logger;
        }

        public Task InitializeAsync()
        {
            _client.SlashCommandExecuted += HandleSlashCommandAsync;
            return Task.CompletedTask;
        }

        private async Task HandleSlashCommandAsync(SocketSlashCommand command)
        {
            try
            {
                switch (command.Data.Name)
                {
                    case "addmonitor":
                        await HandleAddMonitorCommand(command);
                        break;
                    case "updatemonitor":
                        await HandleUpdateMonitorCommand(command);
                        break;
                    case "removemonitor":
                        await HandleRemoveMonitorCommand(command);
                        break;
                    case "viewmonitors":
                        await HandleViewMonitorsCommand(command);
                        break;
                    default:
                        await command.RespondAsync("Unknown command.", ephemeral: true);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling slash command {CommandName}", command.Data.Name);
                await command.RespondAsync("An error occurred while processing the command.", ephemeral: true);
            }
        }

        private async Task HandleAddMonitorCommand(SocketSlashCommand command)
        {
            var market = (string)command.Data.Options.First(o => o.Name == "market").Value;
            var worldName = (string)command.Data.Options.First(o => o.Name == "worldname").Value;
            var minPoints = Convert.ToInt32(command.Data.Options.First(o => o.Name == "minpoints").Value);
            var continent = command.Data.Options.FirstOrDefault(o => o.Name == "continent")?.Value as string;

            var monitor = new Monitor
            {
                Id = Guid.NewGuid().ToString(),
                Market = market,
                WorldName = worldName,
                MinPoints = minPoints,
                Continent = continent,
                ChannelId = command.ChannelId
            };

            _monitorService.AddMonitor(monitor);
            await command.RespondAsync($"Monitor added with ID: {monitor.Id}", ephemeral: true);
        }

        private async Task HandleUpdateMonitorCommand(SocketSlashCommand command)
        {
            var id = (string)command.Data.Options.First(o => o.Name == "id").Value;
            var market = (string)command.Data.Options.First(o => o.Name == "market").Value;
            var worldName = (string)command.Data.Options.First(o => o.Name == "worldname").Value;
            var minPoints = Convert.ToInt32(command.Data.Options.First(o => o.Name == "minpoints").Value);
            var continent = command.Data.Options.FirstOrDefault(o => o.Name == "continent")?.Value as string;

            var updatedMonitor = new Monitor
            {
                Id = id,
                Market = market,
                WorldName = worldName,
                MinPoints = minPoints,
                Continent = continent,
                ChannelId = command.ChannelId
            };

            _monitorService.UpdateMonitor(id, updatedMonitor);
            await command.RespondAsync($"Monitor updated: {id}", ephemeral: true);
        }

        private async Task HandleRemoveMonitorCommand(SocketSlashCommand command)
        {
            var id = (string)command.Data.Options.First(o => o.Name == "id").Value;
            _monitorService.RemoveMonitor(id);
            await command.RespondAsync($"Monitor removed: {id}", ephemeral: true);
        }

        private async Task HandleViewMonitorsCommand(SocketSlashCommand command)
        {
            var monitors = _monitorService.GetAllMonitors();

            if (monitors.Count == 0)
            {
                await command.RespondAsync("There are no active monitors.", ephemeral: true);
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("📊 Active Monitors")
                .WithColor(Color.Blue)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .WithFooter(footer => footer.Text = "TribalSentry Bot");

            foreach (var monitor in monitors)
            {
                embed.AddField(
                    $"Monitor: {monitor.Id}",
                    $"Market: {monitor.Market}\n" +
                    $"World: {monitor.WorldName}\n" +
                    $"Min Points: {monitor.MinPoints}\n" +
                    $"Continent: {monitor.Continent ?? "All"}\n" +
                    $"Channel: <#{monitor.ChannelId}>",
                    inline: false
                );
            }

            // Generate JSON file with known barbarian villages for all monitors
            var jsonFileName = "known_barb_villages.json";
            var jsonFilePath = Path.Combine(Path.GetTempPath(), jsonFileName);
            var knownVillages = new Dictionary<string, List<Village>>();

            foreach (var monitor in monitors)
            {
                var villages = await GetKnownBarbarianVillagesAsync(monitor);
                knownVillages[monitor.Id] = villages;
            }

            await File.WriteAllTextAsync(jsonFilePath, JsonSerializer.Serialize(knownVillages, new JsonSerializerOptions { WriteIndented = true }));

            // Send the embed and file
            await command.RespondWithFileAsync(jsonFilePath, embed: embed.Build(), ephemeral: true);

            // Clean up the temporary file
            File.Delete(jsonFilePath);
        }

        private async Task<List<Village>> GetKnownBarbarianVillagesAsync(Monitor monitor)
        {
            return await _monitorService.GetCurrentBarbarianVillagesAsync(monitor);
        }
    }
}