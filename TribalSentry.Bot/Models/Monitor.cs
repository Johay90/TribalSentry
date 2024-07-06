namespace TribalSentry.Bot.Models;

public class Monitor
{
    public string Id { get; set; }
    public string Market { get; set; }
    public string WorldName { get; set; }
    public int MinPoints { get; set; }
    public string Continent { get; set; }
    public ulong? ChannelId { get; set; }
}