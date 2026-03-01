using Facette.IntegrationTests.Dtos;
using Facette.IntegrationTests.Models;
using Xunit;

namespace Facette.IntegrationTests;

public class EnumConversionIntegrationTests
{
    [Fact]
    public void FromSource_EnumToString_ConvertsCorrectly()
    {
        var order = new Order
        {
            Id = 1,
            OrderDate = DateTime.Now,
            Status = OrderStatus.Shipped
        };

        var dto = OrderStatusDto.FromSource(order);

        Assert.Equal("Shipped", dto.StatusText);
    }

    [Fact]
    public void ToSource_StringToEnum_ConvertsBack()
    {
        var order = new Order
        {
            Id = 2,
            OrderDate = DateTime.Now,
            Status = OrderStatus.Delivered
        };

        var dto = OrderStatusDto.FromSource(order);
        var roundTripped = dto.ToSource();

        Assert.Equal(OrderStatus.Delivered, roundTripped.Status);
    }

    [Fact]
    public void FromSource_EnumToString_AllValues()
    {
        foreach (var status in Enum.GetValues<OrderStatus>())
        {
            var order = new Order
            {
                Id = 1,
                OrderDate = DateTime.Now,
                Status = status
            };

            var dto = OrderStatusDto.FromSource(order);
            Assert.Equal(status.ToString(), dto.StatusText);
        }
    }
}
