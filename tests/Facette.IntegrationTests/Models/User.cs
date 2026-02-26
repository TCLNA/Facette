namespace Facette.IntegrationTests.Models;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public Address? HomeAddress { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}
