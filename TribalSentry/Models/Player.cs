namespace TribalSentry.API.Models;

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int TribeId { get; set; }
    public int Villages { get; set; }
    public int Points { get; set; }
    public int Rank { get; set; }
    public Tribe Tribe { get; set; }
}