using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System.Text;
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

            var sb = new StringBuilder();
            sb.AppendLine("Active Monitors:");

            foreach (var monitor in monitors)
            {
                sb.AppendLine($"- ID: {monitor.Id}");
                sb.AppendLine($"  Market: {monitor.Market}");
                sb.AppendLine($"  World: {monitor.WorldName}");
                sb.AppendLine($"  Min Points: {monitor.MinPoints}");
                sb.AppendLine($"  Continent: {monitor.Continent ?? "All"}");
                sb.AppendLine($"  Channel: <#{monitor.ChannelId}>");
                sb.AppendLine();
            }

            await command.RespondAsync(sb.ToString(), ephemeral: true);
        }
    }
}