namespace Mockups.Services.Analytics
{
    public record EndpointUsage(string Path, long Count, double AverageDurationMs);

    public record AnalyticsUsageResponse(DateTime StartedAtUtc, long TotalRequests, IReadOnlyCollection<EndpointUsage> TopEndpoints);

    public record StatusCodeCount(int StatusCode, long Count);

    public record AnalyticsErrorsResponse(long TotalErrors, long Total4xx, long Total5xx, IReadOnlyCollection<StatusCodeCount> StatusCodeCounts);

    public record AnalyticsSummaryResponse(long TotalUsers, long TotalMenuItems, long TotalOrders, long OrdersLast7Days, double AverageOrderCost);
}
