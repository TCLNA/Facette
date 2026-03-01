namespace Facette.IntegrationTests.Models;

public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public Address ShippingAddress { get; set; } = new();
    public List<OrderItem> Items { get; set; } = new();
    public OrderStatus Status { get; set; }
}
