using TribalSentry.API.Models;

namespace TribalSentry.API.Services;

public interface ITribalWarsService
{
    Task<IEnumerable<Village>> GetVillagesAsync(World world);
    Task<IEnumerable<Village>> GetBarbarianVillagesAsync(World world, string continent = null);
    Task<IEnumerable<Player>> GetPlayersAsync(World world);
    Task<IEnumerable<Tribe>> GetTribesAsync(World world);
    Task<IEnumerable<Conquer>> GetConquersAsync(World world);
    Task<IEnumerable<KillStats>> GetKillStatsAsync(World world, string type);
}