using System.Threading;
using System.Threading.Tasks;

namespace Mockups.Services.Analytics
{
    public interface IAnalyticsService
    {
        Task<AnalyticsSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);

        AnalyticsUsageResponse GetUsage();

        AnalyticsErrorsResponse GetErrors();
    }
}
