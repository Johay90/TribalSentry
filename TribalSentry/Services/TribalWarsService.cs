// TribalSentry.API/Services/TribalWarsService.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TribalSentry.API.Models;

namespace TribalSentry.API.Services
{
    public class TribalWarsService : ITribalWarsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TribalWarsService> _logger;

        public TribalWarsService(HttpClient httpClient, ILogger<TribalWarsService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<IEnumerable<Village>> GetVillagesAsync(World world)
        {
            var content = await FetchContentAsync(world, "village.txt");
            return ParseVillages(content);
        }

        public async Task<IEnumerable<Village>> GetBarbarianVillagesAsync(World world, string continent = null)
        {
            var content = await FetchContentAsync(world, "village.txt");
            var villages = ParseVillages(content).ToList();
            _logger.LogInformation($"Fetched {villages.Count} villages for world {world.Name}");

            var barbarianVillages = villages.Where(v => v.PlayerId == 0).ToList();
            _logger.LogInformation($"Found {barbarianVillages.Count} barbarian villages");

            if (!string.IsNullOrEmpty(continent))
            {
                barbarianVillages = barbarianVillages.Where(v => 
                    v.Continent.Equals(continent, StringComparison.OrdinalIgnoreCase)).ToList();
                _logger.LogInformation($"Filtered to {barbarianVillages.Count} barbarian villages in continent {continent}");
            }

            return barbarianVillages;
        }

        public async Task<IEnumerable<Player>> GetPlayersAsync(World world)
        {
            var playerContent = await FetchContentAsync(world, "player.txt");
            var tribeContent = await FetchContentAsync(world, "ally.txt");

            var tribes = ParseTribes(tribeContent).ToDictionary(t => t.Id);
            var players = ParsePlayers(playerContent).ToList();

            foreach (var player in players)
            {
                if (player.TribeId != 0 && tribes.TryGetValue(player.TribeId, out var tribe))
                {
                    player.Tribe = tribe;
                }
                else
                {
                    player.Tribe = null;
                }
            }

            return players;
        }

        public async Task<IEnumerable<Tribe>> GetTribesAsync(World world)
        {
            var content = await FetchContentAsync(world, "ally.txt");
            return ParseTribes(content);
        }

        public async Task<IEnumerable<Conquer>> GetConquersAsync(World world)
        {
            var content = await FetchContentAsync(world, "conquer.txt");
            return ParseConquers(content);
        }

        public async Task<IEnumerable<KillStats>> GetKillStatsAsync(World world, string type)
        {
            var content = await FetchContentAsync(world, $"kill_{type}.txt");
            return ParseKillStats(content);
        }

        private async Task<string> FetchContentAsync(World world, string file)
        {
            var url = $"https://{world.Name}.{world.Market}/map/{file}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentEncoding.Contains("gzip"))
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
                using var reader = new StreamReader(gzipStream);
                return await reader.ReadToEndAsync();
            }

            return await response.Content.ReadAsStringAsync();
        }

        private IEnumerable<Village> ParseVillages(string content)
        {
            var villages = content.Split('\n')
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line =>
                {
                    var parts = line.Split(',');
                    if (parts.Length < 7)
                    {
                        _logger.LogWarning($"Invalid village data: {line}");
                        return null;
                    }

                    if (!int.TryParse(parts[2], out int x) || !int.TryParse(parts[3], out int y))
                    {
                        _logger.LogWarning($"Invalid coordinates in village data: {line}");
                        return null;
                    }

                    return new Village
                    {
                        Id = int.Parse(parts[0]),
                        Name = parts[1].Replace("+", " "),
                        X = x,
                        Y = y,
                        PlayerId = int.Parse(parts[4]),
                        Points = int.Parse(parts[5]),
                        Rank = int.Parse(parts[6]),
                        Continent = CalculateContinent(x, y)
                    };
                })
                .Where(v => v != null)
                .ToList();

            _logger.LogInformation($"Parsed {villages.Count} valid villages");
            return villages;
        }

        private string CalculateContinent(int x, int y)
        {
            int kx = (x / 100);
            int ky = (y / 100);
            return $"K{kx}{ky}";
        }

        private IEnumerable<Player> ParsePlayers(string content)
        {
            return content.Split('\n')
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line =>
                {
                    var parts = line.Split(',');
                    return new Player
                    {
                        Id = int.Parse(parts[0]),
                        Name = parts[1].Replace("+", " "),
                        TribeId = int.Parse(parts[2]),
                        Villages = int.Parse(parts[3]),
                        Points = int.Parse(parts[4]),
                        Rank = int.Parse(parts[5]),
                        Tribe = null
                    };
                });
        }

        private IEnumerable<Tribe> ParseTribes(string content)
        {
            return content.Split('\n')
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line =>
                {
                    var parts = line.Split(',');
                    return new Tribe
                    {
                        Id = int.Parse(parts[0]),
                        Name = parts[1].Replace("+", " "),
                        Tag = parts[2],
                        Members = int.Parse(parts[3]),
                        Villages = int.Parse(parts[4]),
                        Points = int.Parse(parts[5]),
                        AllPoints = int.Parse(parts[6]),
                        Rank = int.Parse(parts[7])
                    };
                });
        }

        private IEnumerable<Conquer> ParseConquers(string content)
        {
            return content.Split('\n')
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line =>
                {
                    var parts = line.Split(',');
                    return new Conquer
                    {
                        VillageId = int.Parse(parts[0]),
                        Timestamp = long.Parse(parts[1]),
                        NewOwnerId = int.Parse(parts[2]),
                        OldOwnerId = int.Parse(parts[3])
                    };
                });
        }

        private IEnumerable<KillStats> ParseKillStats(string content)
        {
            return content.Split('\n')
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line =>
                {
                    var parts = line.Split(',');
                    return new KillStats
                    {
                        Rank = int.Parse(parts[0]),
                        Id = int.Parse(parts[1]),
                        Kills = int.Parse(parts[2])
                    };
                });
        }
    }
}