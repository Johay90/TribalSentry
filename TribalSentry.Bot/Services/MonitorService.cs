using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using TribalSentry.Bot.Models;
using Monitor = TribalSentry.Bot.Models.Monitor;

namespace TribalSentry.Bot.Services
{
    public class MonitorService
    {
        private readonly TribalWarsApiService _apiService;
        private readonly DiscordSocketClient _client;
        private readonly ILogger<MonitorService> _logger;
        private readonly List<Monitor> _monitors = new List<Monitor>();
        private readonly Dictionary<string, HashSet<int>> _knownVillages = new Dictionary<string, HashSet<int>>();
        private bool _isInitialLoad = true;

        public MonitorService(TribalWarsApiService apiService, DiscordSocketClient client, ILogger<MonitorService> logger)
        {
            _apiService = apiService;
            _client = client;
            _logger = logger;
        }

        public async Task StartMonitoringAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var monitor in _monitors)
                {
                    await CheckForVillageChangesAsync(monitor);
                }

                if (_isInitialLoad)
                {
                    _isInitialLoad = false;
                    _logger.LogInformation("Initial village load completed.");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            }
        }

        public void AddMonitor(Monitor monitor)
        {
            _monitors.Add(monitor);
            _knownVillages[$"{monitor.Market}_{monitor.WorldName}"] = new HashSet<int>();
            _isInitialLoad = true;
        }

        public void RemoveMonitor(string id)
        {
            var monitor = _monitors.FirstOrDefault(m => m.Id == id);
            if (monitor != null)
            {
                _monitors.Remove(monitor);
                _knownVillages.Remove($"{monitor.Market}_{monitor.WorldName}");
            }
        }

        public void UpdateMonitor(string id, Monitor updatedMonitor)
        {
            var index = _monitors.FindIndex(m => m.Id == id);
            if (index != -1)
            {
                _monitors[index] = updatedMonitor;
            }
        }

        public List<Monitor> GetAllMonitors()
        {
            return _monitors;
        }

        private async Task CheckForVillageChangesAsync(Monitor monitor)
        {
            try
            {
                var allVillages = await _apiService.GetAllVillagesAsync(monitor.Market, monitor.WorldName);
                var barbarianVillages = allVillages.Where(v => v.PlayerId == 0).ToList();
                var filteredBarbarianVillages = barbarianVillages
                    .Where(v => v.Points >= monitor.MinPoints &&
                                (string.IsNullOrEmpty(monitor.Continent) || v.Continent == monitor.Continent))
                    .ToList();

                var key = $"{monitor.Market}_{monitor.WorldName}";

                if (_isInitialLoad)
                {
                    // During initial load, just add all villages to known list without notifications
                    foreach (var village in filteredBarbarianVillages)
                    {
                        _knownVillages[key].Add(village.Id);
                    }
                    _logger.LogInformation($"Initial load: Added {filteredBarbarianVillages.Count} villages to known list for {key}");
                }
                else
                {
                    // Check for new barbarian villages
                    var newVillages = filteredBarbarianVillages.Where(v => !_knownVillages[key].Contains(v.Id)).ToList();
                    foreach (var village in newVillages)
                    {
                        _knownVillages[key].Add(village.Id);
                        await NotifyNewBarbarianVillageAsync(monitor, village);
                    }

                    // Check for conquered barbarian villages
                    var conqueredVillages = _knownVillages[key].Where(id => !barbarianVillages.Any(v => v.Id == id)).ToList();
                    foreach (var villageId in conqueredVillages)
                    {
                        _knownVillages[key].Remove(villageId);
                        var conqueredVillage = allVillages.FirstOrDefault(v => v.Id == villageId);
                        if (conqueredVillage != null)
                        {
                            await NotifyConqueredBarbarianVillageAsync(monitor, conqueredVillage);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking for village changes: {ex.Message}");
            }
        }

        private async Task NotifyNewBarbarianVillageAsync(Monitor monitor, Village village)
        {
            var channel = _client.GetChannel((ulong)monitor.ChannelId) as ISocketMessageChannel;
            if (channel != null)
            {
                var message = $"New barbarian village: Points: {village.Points} Link: [{village.X}|{village.Y}](https://{monitor.WorldName}.{monitor.Market}/game.php?screen=info_village&id={village.Id})";
                await channel.SendMessageAsync(message);
            }
        }

        private async Task NotifyConqueredBarbarianVillageAsync(Monitor monitor, Village village)
        {
            var channel = _client.GetChannel((ulong)monitor.ChannelId) as ISocketMessageChannel;
            if (channel != null)
            {
                var message = $"Barbarian village conquered: Points: {village.Points} Link: [{village.X}|{village.Y}](https://{monitor.WorldName}.{monitor.Market}/game.php?screen=info_village&id={village.Id})";
                await channel.SendMessageAsync(message);
            }
        }
    }
}