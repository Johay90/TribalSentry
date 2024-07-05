namespace TribalSentry.API.Models;

public class Tribe
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Tag { get; set; }
    public int Members { get; set; }
    public int Villages { get; set; }
    public int Points { get; set; }
    public int AllPoints { get; set; }
    public int Rank { get; set; }
}