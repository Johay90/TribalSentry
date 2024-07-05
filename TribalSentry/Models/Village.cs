namespace TribalSentry.API.Models;

public class Village
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int PlayerId { get; set; }
    public int Points { get; set; }
    public int Rank { get; set; }
    public string Continent { get; set; }
}