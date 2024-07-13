using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TribalSentry.API.Models;
using TribalSentry.API.Services;

namespace TribalSentry.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TribalWarsController : ControllerBase
{
    private readonly TribalWarsCacheService _cacheService;
    private readonly ILogger<TribalWarsController> _logger;

    public TribalWarsController(TribalWarsCacheService cacheService, ILogger<TribalWarsController> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    [HttpGet("villages")]
    public async Task<IActionResult> GetVillages([FromQuery] string market, [FromQuery] string worldName, [FromQuery] int? minPoints, [FromQuery] int? maxPoints)
    {
        var world = new World { Market = market, Name = worldName };
        var villages = await _cacheService.GetVillagesAsync(world);

        if (minPoints.HasValue)
        {
            villages = villages.Where(v => v.Points >= minPoints.Value);
        }

        if (maxPoints.HasValue)
        {
            villages = villages.Where(v => v.Points <= maxPoints.Value);
        }

        return Ok(villages);
    }

    [HttpGet("barbarian-villages")]
    public async Task<IActionResult> GetBarbarianVillages([FromQuery] string market, [FromQuery] string worldName, [FromQuery] string continent)
    {
        var world = new World { Market = market, Name = worldName };
        var barbarianVillages = await _cacheService.GetBarbarianVillagesAsync(world, continent);
        _logger.LogInformation($"Returning {barbarianVillages.Count()} barbarian villages for world {worldName}, continent {continent ?? "all"}");
        return Ok(barbarianVillages);
    }

    [HttpGet("players")]
    public async Task<IActionResult> GetPlayers([FromQuery] string market, [FromQuery] string worldName)
    {
        var world = new World { Market = market, Name = worldName };
        var players = await _cacheService.GetPlayersAsync(world);
        return Ok(players);
    }

    [HttpGet("tribes")]
    public async Task<IActionResult> GetTribes([FromQuery] string market, [FromQuery] string worldName)
    {
        var world = new World { Market = market, Name = worldName };
        var tribes = await _cacheService.GetTribesAsync(world);
        return Ok(tribes);
    }

    [HttpGet("conquers")]
    public async Task<IActionResult> GetConquers([FromQuery] string market, [FromQuery] string worldName)
    {
        var world = new World { Market = market, Name = worldName };
        var conquers = await _cacheService.GetConquersAsync(world);
        return Ok(conquers);
    }
}