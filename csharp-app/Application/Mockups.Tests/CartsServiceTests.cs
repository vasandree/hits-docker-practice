using Mockups.Repositories.Carts;
using Mockups.Repositories.MenuItems;
using Mockups.Services.Carts;
using Mockups.Storage;
using Xunit;

namespace Mockups.Tests;

public class CartsServiceTests
{
    [Fact]
    public async Task AddItemToCart_ThrowsWhenItemMissing()
    {
        using var context = TestDbFactory.CreateContext();
        var menuRepository = new MenuItemRepository(context);
        var cartsRepository = new CartsRepository();
        var service = new CartsService(menuRepository, cartsRepository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AddItemToCart(Guid.NewGuid(), Guid.NewGuid().ToString(), 1));
    }

    [Fact]
    public async Task GetUsersCart_ReturnsItemsWithAmounts()
    {
        using var context = TestDbFactory.CreateContext();
        var menuItem = new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Latte",
            Price = 4.5f,
            Category = MenuItemCategory.Drink,
            Description = "Test",
            IsVegan = true,
            PhotoPath = "latte.png",
            IsDeleted = false
        };
        await context.MenuItems.AddAsync(menuItem);
        await context.SaveChangesAsync();

        var menuRepository = new MenuItemRepository(context);
        var cartsRepository = new CartsRepository();
        var service = new CartsService(menuRepository, cartsRepository);

        var userId = Guid.NewGuid();
        cartsRepository.AddItemToCart(userId, new CartMenuItem { MenuItemId = menuItem.Id, Amount = 2 });

        var cart = await service.GetUsersCart(userId);

        Assert.Single(cart.Items);
        Assert.Equal(menuItem.Id, cart.Items[0].Id);
        Assert.Equal("Latte", cart.Items[0].Name);
        Assert.Equal(2, cart.Items[0].Amount);
    }
}
