using Facette.Sample.Dtos;
using Facette.Sample.Models;

var product = new Product
{
    Id = 1,
    Name = "Widget",
    Description = "A fine widget",
    Price = 9.99m,
    InternalSku = "WDG-001",
    CreatedAt = DateTime.UtcNow
};

// Using inline method
var dto = ProductDto.FromSource(product);
Console.WriteLine($"DTO: {dto.Id} - {dto.Name} - {dto.Price:C}");

// Using extension method
var dto2 = product.ToDto();
Console.WriteLine($"Extension: {dto2.Id} - {dto2.Name}");

// Round-trip
var backToProduct = dto.ToSource();
Console.WriteLine($"Round-trip: {backToProduct.Id} - {backToProduct.Name} - {backToProduct.Price:C}");

// Queryable projection
var products = new List<Product> { product }.AsQueryable();
var projected = products.ProjectToDto().ToList();
Console.WriteLine($"Projected: {projected.Count} items");

Console.WriteLine("\nFacette is working!");
