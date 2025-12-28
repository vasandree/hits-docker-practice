namespace Mockups.Services.Analytics
{
    public interface IAnalyticsCollector
    {
        DateTime StartedAtUtc { get; }

        void RecordRequest(string path, int statusCode, long durationMs);

        AnalyticsUsageResponse GetUsage(int top = 5);

        AnalyticsErrorsResponse GetErrors();
    }
}
