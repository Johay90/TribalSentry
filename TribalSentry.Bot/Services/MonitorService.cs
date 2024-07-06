using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TribalSentry.Bot.Models;
using System.Collections.Concurrent;
using Monitor = TribalSentry.Bot.Models.Monitor;

namespace TribalSentry.Bot.Services
{
    public class MonitorService
    {
        private readonly TribalWarsApiService _apiService;
        private readonly DiscordSocketClient _client;
        private readonly ILogger<MonitorService> _logger;
        private List<Monitor> _monitors = new List<Monitor>();
        private readonly ConcurrentDictionary<string, HashSet<int>> _knownVillages = new ConcurrentDictionary<string, HashSet<int>>();
        private readonly ConcurrentDictionary<string, bool> _isInitialLoad = new ConcurrentDictionary<string, bool>();
        private const string MonitorsFilePath = "monitors.json";

        public MonitorService(TribalWarsApiService apiService, DiscordSocketClient client, ILogger<MonitorService> logger)
        {
            _apiService = apiService;
            _client = client;
            _logger = logger;
            LoadMonitors();
        }

        public async Task StartMonitoringAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var monitor in _monitors)
                {
                    await CheckForVillageChangesAsync(monitor);
                }
                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            }
        }

        public void AddMonitor(Monitor monitor)
        {
            _monitors.Add(monitor);
            var key = GetMonitorKey(monitor);
            _knownVillages.TryAdd(key, new HashSet<int>());
            _isInitialLoad[key] = true;
            SaveMonitors();
        }

        public void RemoveMonitor(string id)
        {
            var monitor = _monitors.FirstOrDefault(m => m.Id == id);
            if (monitor != null)
            {
                _monitors.Remove(monitor);
                var key = GetMonitorKey(monitor);
                _knownVillages.TryRemove(key, out _);
                _isInitialLoad.TryRemove(key, out _);
                SaveMonitors();
            }
        }

        public void UpdateMonitor(string id, Monitor updatedMonitor)
        {
            var index = _monitors.FindIndex(m => m.Id == id);
            if (index != -1)
            {
                var oldMonitor = _monitors[index];
                var oldKey = GetMonitorKey(oldMonitor);
                var newKey = GetMonitorKey(updatedMonitor);

                _monitors[index] = updatedMonitor;

                if (oldKey != newKey)
                {
                    _knownVillages.TryRemove(oldKey, out _);
                    _isInitialLoad.TryRemove(oldKey, out _);
                    _knownVillages.TryAdd(newKey, new HashSet<int>());
                    _isInitialLoad[newKey] = true;
                }

                SaveMonitors();
            }
        }

        public List<Monitor> GetAllMonitors()
        {
            return _monitors;
        }

        private void SaveMonitors()
        {
            try
            {
                var json = JsonSerializer.Serialize(_monitors);
                File.WriteAllText(MonitorsFilePath, json);
                _logger.LogInformation("Monitors saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving monitors to file.");
            }
        }

        private void LoadMonitors()
        {
            if (File.Exists(MonitorsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(MonitorsFilePath);
                    _monitors = JsonSerializer.Deserialize<List<Monitor>>(json);
                    _logger.LogInformation($"Loaded {_monitors.Count} monitors from file.");

                    foreach (var monitor in _monitors)
                    {
                        var key = GetMonitorKey(monitor);
                        _knownVillages.TryAdd(key, new HashSet<int>());
                        _isInitialLoad[key] = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading monitors from file.");
                    _monitors = new List<Monitor>();
                }
            }
            else
            {
                _logger.LogInformation("No saved monitors found. Starting with an empty list.");
                _monitors = new List<Monitor>();
            }
        }

        private async Task CheckForVillageChangesAsync(Monitor monitor)
        {
            try
            {
                var barbarianVillages = await _apiService.GetBarbarianVillagesAsync(monitor.Market, monitor.WorldName, monitor.Continent);
                _logger.LogInformation($"Fetched {barbarianVillages.Count()} barbarian villages for {monitor.Market} {monitor.WorldName} {monitor.Continent}");

                var filteredVillages = barbarianVillages
                    .Where(v => v.Points >= monitor.MinPoints)
                    .ToList();

                _logger.LogInformation($"Filtered to {filteredVillages.Count} villages meeting criteria");

                var key = GetMonitorKey(monitor);
                var knownVillages = _knownVillages.GetOrAdd(key, new HashSet<int>());
                var isInitialLoad = _isInitialLoad.GetOrAdd(key, true);

                var newVillages = filteredVillages.Where(v => !knownVillages.Contains(v.Id)).ToList();
                foreach (var village in newVillages)
                {
                    knownVillages.Add(village.Id);
                    _logger.LogInformation($"New barbarian village: ID={village.Id}, X={village.X}, Y={village.Y}, Points={village.Points}");
                    if (!isInitialLoad)
                    {
                        await NotifyNewBarbarianVillageAsync(monitor, village);
                    }
                }

                // Remove villages that are no longer barbarian or don't meet criteria
                var villagesToRemove = knownVillages.Except(filteredVillages.Select(v => v.Id)).ToList();
                foreach (var villageId in villagesToRemove)
                {
                    knownVillages.Remove(villageId);
                    _logger.LogInformation($"Removed village with ID {villageId} from tracking");
                }

                _logger.LogInformation($"After processing, tracking {knownVillages.Count} villages for {key}");

                if (isInitialLoad)
                {
                    _isInitialLoad[key] = false;
                    _logger.LogInformation($"Initial load completed for {key}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking for village changes: {ex.Message}");
            }
        }

        private string GetMonitorKey(Monitor monitor)
        {
            return $"{monitor.Market}_{monitor.WorldName}_{monitor.Continent?.ToLower() ?? "all"}";
        }

        private async Task NotifyNewBarbarianVillageAsync(Monitor monitor, Village village)
        {
            var channel = _client.GetChannel((ulong)monitor.ChannelId) as ISocketMessageChannel;
            if (channel != null)
            {
                var message = $"New barbarian village in {monitor.WorldName} {monitor.Continent}: Points: {village.Points} Link: [{village.X}|{village.Y}](https://{monitor.WorldName}.{monitor.Market}/game.php?screen=info_village&id={village.Id})";
                await channel.SendMessageAsync(message);
            }
        }
    }
}