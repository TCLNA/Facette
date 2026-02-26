namespace Facette.Sample.Models;

public class Review
{
    public int Id { get; set; }
    public string Author { get; set; } = "";
    public string Text { get; set; } = "";
    public int Rating { get; set; }
}
