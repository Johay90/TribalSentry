namespace TribalSentry.API.Models;

public class Conquer
{
    public int VillageId { get; set; }
    public long Timestamp { get; set; }
    public int NewOwnerId { get; set; }
    public int OldOwnerId { get; set; }
}