using Microsoft.EntityFrameworkCore;
using Mockups.Storage;

namespace Mockups.Services.Analytics
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAnalyticsCollector _collector;

        public AnalyticsService(ApplicationDbContext context, IAnalyticsCollector collector)
        {
            _context = context;
            _collector = collector;
        }

        public async Task<AnalyticsSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var lastWeek = now.AddDays(-7);

            var totalUsersTask = _context.Users.LongCountAsync(cancellationToken);
            var totalMenuItemsTask = _context.MenuItems.LongCountAsync(item => !item.IsDeleted, cancellationToken);
            var totalOrdersTask = _context.Orders.LongCountAsync(cancellationToken);
            var ordersLast7DaysTask = _context.Orders.LongCountAsync(order => order.CreationTime >= lastWeek, cancellationToken);
            var averageOrderCostTask = _context.Orders
                .Select(order => (double?)order.Cost)
                .AverageAsync(cancellationToken);

            await Task.WhenAll(totalUsersTask, totalMenuItemsTask, totalOrdersTask, ordersLast7DaysTask, averageOrderCostTask);

            return new AnalyticsSummaryResponse(
                totalUsersTask.Result,
                totalMenuItemsTask.Result,
                totalOrdersTask.Result,
                ordersLast7DaysTask.Result,
                averageOrderCostTask.Result ?? 0);
        }

        public AnalyticsUsageResponse GetUsage()
        {
            return _collector.GetUsage();
        }

        public AnalyticsErrorsResponse GetErrors()
        {
            return _collector.GetErrors();
        }
    }
}
