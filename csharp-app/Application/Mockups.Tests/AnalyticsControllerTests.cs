using Microsoft.AspNetCore.Mvc;
using Mockups.Controllers;
using Mockups.Services.Analytics;
using Mockups.Storage;
using Xunit;

namespace Mockups.Tests;

public class AnalyticsControllerTests
{
    [Fact]
    public async Task Summary_ReturnsDatabaseCounts()
    {
        using var context = TestDbFactory.CreateContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "user1",
            NormalizedUserName = "USER1",
            Name = "Test User",
            Email = "user1@example.com",
            Phone = "123",
            BirthDate = DateTime.UtcNow
        };
        await context.Users.AddAsync(user);
        await context.MenuItems.AddAsync(new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Latte",
            Price = 4.5f,
            Category = MenuItemCategory.Drink,
            Description = "Test",
            IsVegan = true,
            PhotoPath = "latte.png",
            IsDeleted = false
        });
        await context.MenuItems.AddAsync(new MenuItem
        {
            Id = Guid.NewGuid(),
            Name = "Hidden",
            Price = 1.0f,
            Category = MenuItemCategory.Snack,
            Description = "Test",
            IsVegan = false,
            PhotoPath = "hidden.png",
            IsDeleted = true
        });
        await context.Orders.AddAsync(new Order
        {
            CreationTime = DateTime.UtcNow,
            DeliveryTime = DateTime.UtcNow.AddHours(1),
            Cost = 12.5f,
            Discount = 0,
            Address = "Main",
            Status = OrderStatus.Created,
            UserId = user.Id
        });
        await context.SaveChangesAsync();

        var service = new AnalyticsService(context, new AnalyticsCollector());
        var controller = new AnalyticsController(service);

        var result = await controller.Summary(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AnalyticsSummaryResponse>(okResult.Value);

        Assert.Equal(1, payload.TotalUsers);
        Assert.Equal(1, payload.TotalMenuItems);
        Assert.Equal(1, payload.TotalOrders);
        Assert.Equal(1, payload.OrdersLast7Days);
        Assert.Equal(12.5, payload.AverageOrderCost, 1);
    }

    [Fact]
    public void Usage_ReturnsTopEndpoints()
    {
        using var context = TestDbFactory.CreateContext();
        var collector = new AnalyticsCollector();
        var service = new AnalyticsService(context, collector);
        var controller = new AnalyticsController(service);

        collector.RecordRequest("/Menu", 200, 120);
        collector.RecordRequest("/Menu", 200, 80);
        collector.RecordRequest("/Orders", 200, 200);

        var result = controller.Usage();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AnalyticsUsageResponse>(okResult.Value);

        Assert.Equal(3, payload.TotalRequests);
        Assert.NotEmpty(payload.TopEndpoints);
        var topEndpoint = payload.TopEndpoints.First();
        Assert.Equal("/Menu", topEndpoint.Path);
        Assert.Equal(2, topEndpoint.Count);
        Assert.True(topEndpoint.AverageDurationMs > 0);
    }

    [Fact]
    public void Errors_ReturnsStatusCounts()
    {
        using var context = TestDbFactory.CreateContext();
        var collector = new AnalyticsCollector();
        var service = new AnalyticsService(context, collector);
        var controller = new AnalyticsController(service);

        collector.RecordRequest("/Menu", 500, 10);
        collector.RecordRequest("/Menu", 404, 10);
        collector.RecordRequest("/Menu", 200, 10);

        var result = controller.Errors();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AnalyticsErrorsResponse>(okResult.Value);

        Assert.Equal(2, payload.TotalErrors);
        Assert.Equal(1, payload.Total4xx);
        Assert.Equal(1, payload.Total5xx);
        Assert.Contains(payload.StatusCodeCounts, item => item.StatusCode == 404 && item.Count == 1);
        Assert.Contains(payload.StatusCodeCounts, item => item.StatusCode == 500 && item.Count == 1);
    }
}
