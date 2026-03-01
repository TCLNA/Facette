using System.ComponentModel.DataAnnotations;

namespace Facette.Sample.Models;

public class Employee
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = "";

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = "";

    [EmailAddress]
    public string Email { get; set; } = "";

    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; }
    public Department Department { get; set; }
    public Address? HomeAddress { get; set; }
    public bool IsActive { get; set; } = true;
    public string SocialSecurityNumber { get; set; } = "";
    public string Notes { get; set; } = "";
}
