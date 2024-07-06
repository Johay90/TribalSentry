using System.Net.Http.Json;
using TribalSentry.Bot.Models;

namespace TribalSentry.Bot.Services;

public class TribalWarsApiService
{
    private readonly HttpClient _httpClient;

    public TribalWarsApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7171/")
        };
    }

    public async Task<IEnumerable<Village>> GetAllVillagesAsync(string market, string worldName)
    {
        var response = await _httpClient.GetFromJsonAsync<IEnumerable<Village>>($"api/tribalwars/villages?market={market}&worldName={worldName}");
        return response ?? new List<Village>();
    }

    public async Task<IEnumerable<Village>> GetBarbarianVillagesAsync(string market, string worldName, string continent = null)
    {
        var query = $"api/tribalwars/barbarian-villages?market={market}&worldName={worldName}";
        if (!string.IsNullOrEmpty(continent))
        {
            query += $"&continent={continent}";
        }
        var response = await _httpClient.GetFromJsonAsync<IEnumerable<Village>>(query);
        return response ?? new List<Village>();
    }
}