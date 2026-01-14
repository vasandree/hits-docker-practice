using System.Collections.Concurrent;

namespace Mockups.Services.Analytics
{
    public class AnalyticsCollector : IAnalyticsCollector
    {
        private readonly ConcurrentDictionary<string, RequestStats> _requestStats = new();
        private readonly ConcurrentDictionary<int, long> _statusCodeCounts = new();
        private long _totalRequests;

        public DateTime StartedAtUtc { get; } = DateTime.UtcNow;

        public void RecordRequest(string path, int statusCode, long durationMs)
        {
            var normalizedPath = string.IsNullOrWhiteSpace(path) ? "/" : path;
            var stats = _requestStats.GetOrAdd(normalizedPath, _ => new RequestStats());
            stats.Add(durationMs);
            Interlocked.Increment(ref _totalRequests);
            _statusCodeCounts.AddOrUpdate(statusCode, 1, (_, current) => current + 1);
        }

        public AnalyticsUsageResponse GetUsage(int top = 5)
        {
            var topEndpoints = _requestStats
                .Select(pair => new EndpointUsage(
                    pair.Key,
                    pair.Value.Count,
                    pair.Value.AverageDurationMs))
                .OrderByDescending(item => item.Count)
                .ThenBy(item => item.Path)
                .Take(top)
                .ToList();

            return new AnalyticsUsageResponse(StartedAtUtc, Interlocked.Read(ref _totalRequests), topEndpoints);
        }

        public AnalyticsErrorsResponse GetErrors()
        {
            var statusCounts = _statusCodeCounts
                .Where(pair => pair.Key >= 400)
                .OrderBy(pair => pair.Key)
                .Select(pair => new StatusCodeCount(pair.Key, pair.Value))
                .ToList();

            var total4xx = statusCounts.Where(item => item.StatusCode is >= 400 and < 500).Sum(item => item.Count);
            var total5xx = statusCounts.Where(item => item.StatusCode is >= 500).Sum(item => item.Count);
            var totalErrors = total4xx + total5xx;

            return new AnalyticsErrorsResponse(totalErrors, total4xx, total5xx, statusCounts);
        }

        private sealed class RequestStats
        {
            private long _count;
            private long _totalDurationMs;

            public long Count => Interlocked.Read(ref _count);

            public double AverageDurationMs
            {
                get
                {
                    var count = Count;
                    return count == 0 ? 0 : (double)Interlocked.Read(ref _totalDurationMs) / count;
                }
            }

            public void Add(long durationMs)
            {
                Interlocked.Increment(ref _count);
                Interlocked.Add(ref _totalDurationMs, durationMs);
            }
        }
    }
}
