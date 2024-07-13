using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TribalSentry.Bot.Models;
using System.Collections.Concurrent;
using Monitor = TribalSentry.Bot.Models.Monitor;
using Discord;

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

        public Monitor GetMonitor(string id)
        {
            return _monitors.FirstOrDefault(m => m.Id == id);
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
                var confirmedNewVillages = new List<Village>();

                foreach (var village in newVillages)
                {
                    if (await ConfirmBarbarianVillageAsync(monitor, village))
                    {
                        confirmedNewVillages.Add(village);
                        knownVillages.Add(village.Id);
                        _logger.LogInformation($"Confirmed new barbarian village: ID={village.Id}, X={village.X}, Y={village.Y}, Points={village.Points}");

                        if (!isInitialLoad)
                        {
                            await NotifyNewBarbarianVillageAsync(monitor, village);
                        }
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
                var embed = new EmbedBuilder()
                    .WithTitle("🏰 New Barbarian Village Discovered!")
                    .WithDescription($"A new barbarian village has been found in **{monitor.WorldName}** ({monitor.Continent}).")
                    .WithColor(Color.Green)
                    .WithTimestamp(DateTimeOffset.UtcNow)
                    .WithFields(
                        new EmbedFieldBuilder().WithName("Coordinates").WithValue($"[{village.X}|{village.Y}](https://{monitor.WorldName}.{monitor.Market}/game.php?screen=info_village&id={village.Id})").WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Points").WithValue(village.Points).WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Village ID").WithValue(village.Id).WithIsInline(true)
                    )
                    .WithFooter(footer => footer.Text = $"TribalSentry Bot • Monitor: {monitor.Id}")
                    .Build();

                await channel.SendMessageAsync(embed: embed);
            }
        }

        private async Task<bool> ConfirmBarbarianVillageAsync(Monitor monitor, Village village)
        {
            var key = GetMonitorKey(monitor);
            var isInitialLoad = _isInitialLoad.GetOrAdd(key, true);

            if (!isInitialLoad)
            {
                // First check: Verify with GetAllVillages
                var allVillages = await _apiService.GetAllVillagesAsync(monitor.Market, monitor.WorldName);
                var villageInAll = allVillages.FirstOrDefault(v => v.Id == village.Id);
                if (villageInAll == null || villageInAll.PlayerId != 0)
                {
                    _logger.LogWarning($"Village ID={village.Id} is not a barbarian village in all villages list");
                    return false;
                }

                // Second check: Verify with GetBarbarianVillages after a delay
                await Task.Delay(TimeSpan.FromSeconds(30));
                var barbarianVillagesAfterDelay = await _apiService.GetBarbarianVillagesAsync(monitor.Market, monitor.WorldName, monitor.Continent);
                var villageAfterDelay = barbarianVillagesAfterDelay.FirstOrDefault(v => v.Id == village.Id);
                if (villageAfterDelay == null || villageAfterDelay.PlayerId != 0)
                {
                    _logger.LogWarning($"Village ID={village.Id} is no longer a barbarian village after delay");
                    return false;
                }

                // Third check: Verify against recent conquers
                var conquers = await _apiService.GetConquersAsync(monitor.Market, monitor.WorldName);
                var twelveHoursAgo = DateTimeOffset.UtcNow.AddHours(-12).ToUnixTimeSeconds();
                var recentConquer = conquers.FirstOrDefault(c => c.VillageId == village.Id && c.Timestamp >= twelveHoursAgo);

                if (recentConquer != null)
                {
                    _logger.LogWarning($"Village ID={village.Id} was recently conquered at {DateTimeOffset.FromUnixTimeSeconds(recentConquer.Timestamp)}");
                    return false;
                }
            }

            return true;
        }

        public async Task<List<Village>> GetCurrentBarbarianVillagesAsync(Monitor monitor)
        {
            var key = GetMonitorKey(monitor);
            var knownVillages = _knownVillages.GetOrAdd(key, new HashSet<int>());
            var barbarianVillages = await _apiService.GetBarbarianVillagesAsync(monitor.Market, monitor.WorldName, monitor.Continent);
            return barbarianVillages.Where(v => knownVillages.Contains(v.Id)).ToList();
        }

    }


}