using System.Diagnostics;
using Mockups.Services.Analytics;

namespace Mockups.Middleware
{
    public class AnalyticsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAnalyticsCollector _collector;

        public AnalyticsMiddleware(RequestDelegate next, IAnalyticsCollector collector)
        {
            _next = next;
            _collector = collector;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var statusCode = 0;

            try
            {
                await _next(context);
                statusCode = context.Response.StatusCode;
            }
            catch
            {
                statusCode = StatusCodes.Status500InternalServerError;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                var path = context.Request.Path.HasValue ? context.Request.Path.Value! : "/";
                _collector.RecordRequest(path, statusCode, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
