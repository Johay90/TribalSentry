using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using TribalSentry.API.Models;

namespace TribalSentry.API.Services
{
    public class TribalWarsCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ITribalWarsService _tribalWarsService;
        private readonly ILogger<TribalWarsCacheService> _logger;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        public TribalWarsCacheService(IMemoryCache cache, ITribalWarsService tribalWarsService, ILogger<TribalWarsCacheService> logger)
        {
            _cache = cache;
            _tribalWarsService = tribalWarsService;
            _logger = logger;
        }

        public async Task<IEnumerable<Village>> GetVillagesAsync(World world)
        {
            return await GetOrFetchDataAsync($"villages_{world.Market}_{world.Name}", 
                () => _tribalWarsService.GetVillagesAsync(world), TimeSpan.FromHours(1));
        }

        public async Task<IEnumerable<Village>> GetBarbarianVillagesAsync(World world, string continent = null)
        {
            return await GetOrFetchDataAsync($"barbarian_villages_{world.Market}_{world.Name}_{continent}", 
                () => _tribalWarsService.GetBarbarianVillagesAsync(world, continent), TimeSpan.FromHours(1));
        }

        public async Task<IEnumerable<Player>> GetPlayersAsync(World world)
        {
            return await GetOrFetchDataAsync($"players_{world.Market}_{world.Name}", 
                () => _tribalWarsService.GetPlayersAsync(world), TimeSpan.FromHours(1));
        }

        public async Task<IEnumerable<Tribe>> GetTribesAsync(World world)
        {
            return await GetOrFetchDataAsync($"tribes_{world.Market}_{world.Name}", 
                () => _tribalWarsService.GetTribesAsync(world), TimeSpan.FromHours(1));
        }

        public async Task<IEnumerable<Conquer>> GetConquersAsync(World world)
        {
            return await GetOrFetchDataAsync($"conquers_{world.Market}_{world.Name}", 
                () => _tribalWarsService.GetConquersAsync(world), TimeSpan.FromMinutes(1));
        }

        private async Task<T> GetOrFetchDataAsync<T>(string cacheKey, Func<Task<T>> fetchFunction, TimeSpan cacheDuration)
        {
            var now = DateTime.UtcNow;
            if (_cache.TryGetValue(cacheKey, out CacheEntry<T> cachedEntry))
            {
                _logger.LogInformation($"[{now:yyyy-MM-dd HH:mm:ss}] Cache hit for key: {cacheKey}. Data will expire at {cachedEntry.ExpirationTime:yyyy-MM-dd HH:mm:ss}");
                return cachedEntry.Data;
            }

            _logger.LogInformation($"[{now:yyyy-MM-dd HH:mm:ss}] Cache miss for key: {cacheKey}. Fetching fresh data.");

            var lockObj = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
            await lockObj.WaitAsync();
            try
            {
                // Double-check in case another thread already populated the cache
                if (_cache.TryGetValue(cacheKey, out cachedEntry))
                {
                    _logger.LogInformation($"[{now:yyyy-MM-dd HH:mm:ss}] Cache hit after lock for key: {cacheKey}. Data will expire at {cachedEntry.ExpirationTime:yyyy-MM-dd HH:mm:ss}");
                    return cachedEntry.Data;
                }

                var result = await fetchFunction();
                var expirationTime = now.Add(cacheDuration);
                var entry = new CacheEntry<T> { Data = result, ExpirationTime = expirationTime };
                
                _cache.Set(cacheKey, entry, cacheDuration);
                _logger.LogInformation($"[{now:yyyy-MM-dd HH:mm:ss}] Data fetched and cached for key: {cacheKey}. Will expire at {expirationTime:yyyy-MM-dd HH:mm:ss}");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{now:yyyy-MM-dd HH:mm:ss}] Error fetching data for cache key: {cacheKey}");
                throw;
            }
            finally
            {
                lockObj.Release();
            }
        }

        private class CacheEntry<T>
        {
            public T Data { get; set; }
            public DateTime ExpirationTime { get; set; }
        }
    }
}