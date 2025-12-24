using Microsoft.EntityFrameworkCore;
using Moq;
using Mockups.Configs;
using Mockups.Models.Account;
using Mockups.Models.Cart;
using Mockups.Models.Menu;
using Mockups.Models.Orders;
using Mockups.Repositories.Orders;
using Mockups.Services.Addresses;
using Mockups.Services.Carts;
using Mockups.Services.MenuItems;
using Mockups.Services.Orders;
using Mockups.Services.Users;
using Mockups.Storage;
using Xunit;

namespace Mockups.Tests;

public class OrdersServiceTests
{
    [Fact]
    public async Task CreateOrder_SavesOrderAndItems_WithBirthdayDiscount()
    {
        using var context = TestDbFactory.CreateContext();
        var ordersRepository = new OrdersRepository(context);

        var cartsService = new Mock<ICartsService>();
        var usersService = new Mock<IUsersService>();
        var addressesService = new Mock<IAddressesService>();
        var menuItemsService = new Mock<IMenuItemsService>();

        var userId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();

        cartsService
            .Setup(service => service.GetUsersCart(userId, false))
            .ReturnsAsync(new CartIndexViewModel
            {
                Items = new List<CartMenuItemViewModel>
                {
                    new() { Id = menuItemId, Name = "Soup", Amount = 3 }
                }
            });

        menuItemsService
            .Setup(service => service.GetItemModelById(menuItemId.ToString()))
            .ReturnsAsync(new MenuItemViewModel { Id = menuItemId, Name = "Soup", Price = 2.5f });

        usersService
            .Setup(service => service.GetUserInfo(userId))
            .ReturnsAsync(new IndexViewModel
            {
                BirthDate = DateTime.Now,
                Name = "Test",
                Phone = "123",
                Addresses = new List<AddressShortViewModel>()
            });

        var orderConfig = new OrderConfig { MinDeliveryTime = 30, DeliveryTimeStep = 10 };
        var service = new OrdersService(ordersRepository, cartsService.Object, usersService.Object, addressesService.Object, orderConfig, menuItemsService.Object);

        var model = new OrderCreatePostViewModel
        {
            Address = "ул. Пушкина, д. 10",
            DeliveryTime = DateTime.Now.AddHours(2)
        };

        await service.CreateOrder(model, userId);

        var order = await context.Orders.SingleAsync();
        var orderItem = await context.OrderMenuItems.SingleAsync();

        Assert.Equal(userId, order.UserId);
        Assert.Equal(model.Address, order.Address);
        Assert.Equal(OrderStatus.New, order.Status);
        Assert.Equal(7.5f, order.Cost);
        Assert.Equal(15f, order.Discount);
        Assert.Equal(order.Id, orderItem.OrderId);
        Assert.Equal(menuItemId, orderItem.ItemId);
        Assert.Equal(3, orderItem.Amount);
    }

    [Fact]
    public async Task GetCreateOrderViewModel_ReturnsDiscountedPriceAndAddresses()
    {
        using var context = TestDbFactory.CreateContext();
        var ordersRepository = new OrdersRepository(context);

        var cartsService = new Mock<ICartsService>();
        var usersService = new Mock<IUsersService>();
        var addressesService = new Mock<IAddressesService>();
        var menuItemsService = new Mock<IMenuItemsService>();

        var userId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();

        cartsService
            .Setup(service => service.GetUsersCart(userId, true))
            .ReturnsAsync(new CartIndexViewModel
            {
                Items = new List<CartMenuItemViewModel>
                {
                    new() { Id = menuItemId, Name = "Tea", Amount = 1 }
                }
            });

        menuItemsService
            .Setup(service => service.GetItemModelById(menuItemId.ToString()))
            .ReturnsAsync(new MenuItemViewModel { Id = menuItemId, Name = "Tea", Price = 5f });

        usersService
            .Setup(service => service.GetUserInfo(userId))
            .ReturnsAsync(new IndexViewModel
            {
                BirthDate = DateTime.Now.AddYears(-20),
                Name = "Test",
                Phone = "123",
                Addresses = new List<AddressShortViewModel>()
            });

        addressesService
            .Setup(service => service.GetAddressesByUserId(userId))
            .Returns(new List<Address>
            {
                new()
                {
                    StreetName = "Ленина",
                    HouseNumber = "1",
                    FlatNumber = "10",
                    Name = "Home",
                    IsMainAddress = true
                },
                new()
                {
                    StreetName = "Ленина",
                    HouseNumber = "1",
                    FlatNumber = "10",
                    Name = "Home",
                    IsMainAddress = false
                },
                new()
                {
                    StreetName = "Садовая",
                    HouseNumber = "5",
                    FlatNumber = "2",
                    Name = "Office",
                    IsMainAddress = false
                }
            });

        var orderConfig = new OrderConfig { MinDeliveryTime = 30, DeliveryTimeStep = 10 };
        var service = new OrdersService(ordersRepository, cartsService.Object, usersService.Object, addressesService.Object, orderConfig, menuItemsService.Object);

        var result = await service.GetCreateOrderViewModel(userId);

        Assert.Equal(5f, result.GetModel.Price);
        Assert.Equal(15f, result.GetModel.Discount);
        Assert.Equal(2, result.GetModel.Addresses.Count);
        Assert.Equal(5, result.GetModel.DeliveryTimeOptions.Count);
    }

    [Fact]
    public async Task EditOrder_ThrowsWhenOrderMissing()
    {
        using var context = TestDbFactory.CreateContext();
        var ordersRepository = new OrdersRepository(context);

        var service = new OrdersService(
            ordersRepository,
            Mock.Of<ICartsService>(),
            Mock.Of<IUsersService>(),
            Mock.Of<IAddressesService>(),
            new OrderConfig { MinDeliveryTime = 30, DeliveryTimeStep = 10 },
            Mock.Of<IMenuItemsService>());

        var model = new OrderEditPostViewModel
        {
            orderId = 99,
            Status = OrderStatus.Delivered,
            DeliveryTime = DateTime.Now.AddHours(1)
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.EditOrder(model));
    }
}
