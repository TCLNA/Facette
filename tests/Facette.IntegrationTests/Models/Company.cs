namespace Facette.IntegrationTests.Models;

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public Address Headquarters { get; set; } = new();
    public DateTime FoundedAt { get; set; }
}
