using Discord.Commands;
using TribalSentry.Bot.Services;

namespace TribalSentry.Bot.Modules;

public class MonitorModule : ModuleBase<SocketCommandContext>
{
    private readonly MonitorService _monitorService;

    public MonitorModule(MonitorService monitorService)
    {
        _monitorService = monitorService;
    }

    [Command("addmonitor")]
    public async Task AddMonitorAsync(string market, string worldName, int minPoints)
    {
        var monitor = new Models.Monitor
        {
            Id = Guid.NewGuid().ToString(),
            Market = market,
            WorldName = worldName,
            MinPoints = minPoints,
            ChannelId = Context.Channel.Id
        };

        _monitorService.AddMonitor(monitor);
        await ReplyAsync($"Monitor added with ID: {monitor.Id}");
    }

    [Command("updatemonitor")]
    public async Task UpdateMonitorAsync(string id, string market, string worldName, int minPoints)
    {
        var updatedMonitor = new Models.Monitor
        {
            Id = id,
            Market = market,
            WorldName = worldName,
            MinPoints = minPoints,
            ChannelId = Context.Channel.Id
        };

        _monitorService.UpdateMonitor(id, updatedMonitor);
        await ReplyAsync($"Monitor updated: {id}");
    }

    [Command("removemonitor")]
    public async Task RemoveMonitorAsync(string id)
    {
        _monitorService.RemoveMonitor(id);
        await ReplyAsync($"Monitor removed: {id}");
    }
}