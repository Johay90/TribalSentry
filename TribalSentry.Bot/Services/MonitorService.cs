using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TribalSentry.Bot.Models;
using Monitor = TribalSentry.Bot.Models.Monitor;

namespace TribalSentry.Bot.Services
{
    public class MonitorService
    {
        private readonly TribalWarsApiService _apiService;
        private readonly DiscordSocketClient _client;
        private readonly ILogger<MonitorService> _logger;
        private List<Monitor> _monitors = new List<Monitor>();
        private readonly Dictionary<string, HashSet<int>> _knownVillages = new Dictionary<string, HashSet<int>>();
        private bool _isInitialLoad = true;
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
            SaveMonitors();
        }

        public void RemoveMonitor(string id)
        {
            var monitor = _monitors.FirstOrDefault(m => m.Id == id);
            if (monitor != null)
            {
                _monitors.Remove(monitor);
                _knownVillages.Remove($"{monitor.Market}_{monitor.WorldName}");
                SaveMonitors();
            }
        }

        public void UpdateMonitor(string id, Monitor updatedMonitor)
        {
            var index = _monitors.FindIndex(m => m.Id == id);
            if (index != -1)
            {
                _monitors[index] = updatedMonitor;
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

                    // Initialize _knownVillages for each loaded monitor
                    foreach (var monitor in _monitors)
                    {
                        _knownVillages[$"{monitor.Market}_{monitor.WorldName}"] = new HashSet<int>();
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
            var allVillages = await _apiService.GetAllVillagesAsync(monitor.Market, monitor.WorldName);
            _logger.LogInformation($"Fetched {allVillages.Count()} villages for {monitor.Market} {monitor.WorldName}");

            var barbarianVillages = allVillages.Where(v => v.PlayerId == 0).ToList();
            _logger.LogInformation($"Found {barbarianVillages.Count} barbarian villages");

            var filteredBarbarianVillages = barbarianVillages
                .Where(v => v.Points >= monitor.MinPoints &&
                            (string.IsNullOrEmpty(monitor.Continent) || v.Continent.ToLower() == monitor.Continent.ToLower()))
                .ToList();
            _logger.LogInformation($"Filtered to {filteredBarbarianVillages.Count} barbarian villages meeting criteria");

            var key = $"{monitor.Market}_{monitor.WorldName}_{monitor.Continent}";

            if (!_knownVillages.ContainsKey(key))
            {
                _knownVillages[key] = new HashSet<int>();
                _isInitialLoad = true;
            }

            _logger.LogInformation($"Known villages for {key}: {_knownVillages[key].Count}");

            if (_isInitialLoad)
            {
                // During initial load, just add all villages to known list without notifications
                foreach (var village in filteredBarbarianVillages)
                {
                    _knownVillages[key].Add(village.Id);
                    _logger.LogDebug($"Added village to known list: ID={village.Id}, X={village.X}, Y={village.Y}, Points={village.Points}, Continent={village.Continent}");
                }
                _logger.LogInformation($"Initial load: Added {filteredBarbarianVillages.Count} villages to known list for {key}");
                _isInitialLoad = false;
            }
            else
            {
                // Check for new barbarian villages
                var newVillages = filteredBarbarianVillages.Where(v => !_knownVillages[key].Contains(v.Id)).ToList();
                _logger.LogInformation($"Found {newVillages.Count} new barbarian villages");

                foreach (var village in newVillages)
                {
                    _knownVillages[key].Add(village.Id);
                    _logger.LogInformation($"New barbarian village: ID={village.Id}, X={village.X}, Y={village.Y}, Points={village.Points}, PlayerId={village.PlayerId}");
                    await NotifyNewBarbarianVillageAsync(monitor, village);
                }

                // Check for villages that are no longer in the list or no longer barbarian
                var villagesChanged = _knownVillages[key].Where(id => !filteredBarbarianVillages.Any(v => v.Id == id)).ToList();
                _logger.LogInformation($"Found {villagesChanged.Count} villages that changed status");

                foreach (var villageId in villagesChanged)
                {
                    var village = allVillages.FirstOrDefault(v => v.Id == villageId);
                    if (village == null)
                    {
                        _logger.LogWarning($"Village with ID {villageId} not found in allVillages list. It may have been deleted or there might be an API inconsistency.");
                        _knownVillages[key].Remove(villageId);
                    }
                    else if (village.PlayerId != 0)
                    {
                        _logger.LogInformation($"Village conquered: ID={village.Id}, X={village.X}, Y={village.Y}, Points={village.Points}, NewPlayerId={village.PlayerId}");
                        _knownVillages[key].Remove(villageId);
                        await NotifyConqueredBarbarianVillageAsync(monitor, village);
                    }
                    else
                    {
                        _logger.LogWarning($"Village with ID {villageId} is still barbarian but no longer meets filter criteria: Points={village.Points}, Continent={village.Continent}");
                        _knownVillages[key].Remove(villageId);
                    }
                }
            }

            _logger.LogInformation($"After processing, known villages for {key}: {_knownVillages[key].Count}");
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