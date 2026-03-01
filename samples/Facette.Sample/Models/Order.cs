namespace Facette.Sample.Models;

public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = "";
    public OrderStatus Status { get; set; }
    public Address ShippingAddress { get; set; } = new();
    public List<OrderItem> Items { get; set; } = new();
}
