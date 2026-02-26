using Facette.Sample.Dtos;
using Facette.Sample.Models;

var product = new Product
{
    Id = 1,
    Name = "Widget",
    Description = "A fine widget",
    Price = 9.99m,
    InternalSku = "WDG-001",
    CreatedAt = DateTime.UtcNow,
    Category = new Category { Id = 10, Name = "Tools" },
    Reviews = new List<Review>
    {
        new() { Id = 1, Author = "Alice", Text = "Great product!", Rating = 5 },
        new() { Id = 2, Author = "Bob", Text = "Decent quality", Rating = 3 }
    }
};

// Map to DTO
var dto = ProductDto.FromSource(product);
Console.WriteLine($"DTO: {dto.Id} - {dto.Name} - {dto.Price:C}");
Console.WriteLine($"  Category: {dto.Category.Name}");
Console.WriteLine($"  Reviews: {dto.Reviews.Count}");
foreach (var review in dto.Reviews)
{
    Console.WriteLine($"    - {review.Author}: {review.Text} ({review.Rating}/5)");
}

// Round-trip
var backToProduct = dto.ToSource();
Console.WriteLine($"\nRound-trip: {backToProduct.Id} - {backToProduct.Name} - {backToProduct.Price:C}");
Console.WriteLine($"  Category: {backToProduct.Category.Name}");
Console.WriteLine($"  Reviews: {backToProduct.Reviews.Count}");

// Extension method
var dto2 = product.ToDto();
Console.WriteLine($"\nExtension: {dto2.Id} - {dto2.Name}");

// Queryable projection
var products = new List<Product> { product }.AsQueryable();
var projected = products.ProjectToDto().ToList();
Console.WriteLine($"\nProjected: {projected.Count} items");
Console.WriteLine($"  First: {projected[0].Name} in {projected[0].Category.Name} with {projected[0].Reviews.Count} reviews");

Console.WriteLine("\nFacette is working!");
