using Mockups.Services.Orders;
using Mockups.Storage;
using Xunit;

namespace Mockups.Tests;

public class OrderComparerTests
{
    [Fact]
    public void Compare_PrioritizesNewOrders()
    {
        var comparer = new OrderComparer();
        var newOrder = new Order { Status = OrderStatus.New, DeliveryTime = DateTime.Now.AddHours(1), Address = "A" };
        var deliveredOrder = new Order { Status = OrderStatus.Delivered, DeliveryTime = DateTime.Now.AddHours(1), Address = "B" };

        var result = comparer.Compare(newOrder, deliveredOrder);

        Assert.True(result < 0);
    }

    [Fact]
    public void Compare_UsesDeliveryTimeWhenStatusSame()
    {
        var comparer = new OrderComparer();
        var earlyOrder = new Order { Status = OrderStatus.Processing, DeliveryTime = DateTime.Now.AddHours(1), Address = "A" };
        var lateOrder = new Order { Status = OrderStatus.Processing, DeliveryTime = DateTime.Now.AddHours(2), Address = "B" };

        var result = comparer.Compare(earlyOrder, lateOrder);

        Assert.True(result < 0);
    }

    [Fact]
    public void Compare_HandlesNulls()
    {
        var comparer = new OrderComparer();

        Assert.Equal(0, comparer.Compare(null, null));
        Assert.True(comparer.Compare(null, new Order { Address = "A" }) > 0);
        Assert.True(comparer.Compare(new Order { Address = "B" }, null) < 0);
    }
}
