using System.Net.Http.Json;
using TribalSentry.Bot.Models;
using Microsoft.Extensions.Logging;

namespace TribalSentry.Bot.Services;

public class TribalWarsApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TribalWarsApiService> _logger;

    public TribalWarsApiService(ILogger<TribalWarsApiService> logger)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000/")
        };
        _logger = logger;
    }

    public async Task<IEnumerable<Village>> GetAllVillagesAsync(string market, string worldName)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<Village>>($"api/tribalwars/villages?market={market}&worldName={worldName}");
            return response ?? new List<Village>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching villages from API");
            return new List<Village>();
        }
    }

    public async Task<IEnumerable<Village>> GetBarbarianVillagesAsync(string market, string worldName, string continent = null)
    {
        try
        {
            var query = $"api/tribalwars/barbarian-villages?market={market}&worldName={worldName}";
            if (!string.IsNullOrEmpty(continent))
            {
                query += $"&continent={continent}";
            }
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<Village>>(query);
            return response ?? new List<Village>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching barbarian villages from API");
            return new List<Village>();
        }
    }

    public async Task<IEnumerable<Conquer>> GetConquersAsync(string market, string worldName)
    {
        var response = await _httpClient.GetFromJsonAsync<IEnumerable<Conquer>>($"api/tribalwars/conquers?market={market}&worldName={worldName}");
        return response ?? new List<Conquer>();
    }
}