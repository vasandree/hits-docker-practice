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

            var totalUsers = await _context.Users.LongCountAsync(cancellationToken);
            var totalMenuItems = await _context.MenuItems.LongCountAsync(item => !item.IsDeleted, cancellationToken);
            var totalOrders = await _context.Orders.LongCountAsync(cancellationToken);
            var ordersLast7Days = await _context.Orders.LongCountAsync(order => order.CreationTime >= lastWeek, cancellationToken);
            var averageOrderCost = await _context.Orders
                .Select(order => (double?)order.Cost)
                .AverageAsync(cancellationToken);

            return new AnalyticsSummaryResponse(
                totalUsers,
                totalMenuItems,
                totalOrders,
                ordersLast7Days,
                averageOrderCost ?? 0);
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
