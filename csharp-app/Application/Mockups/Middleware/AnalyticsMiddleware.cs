using System.Diagnostics;
using Mockups.Services.Analytics;

namespace Mockups.Middleware
{
    public class AnalyticsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAnalyticsCollector _collector;
        private readonly ILogger<AnalyticsMiddleware> _logger;

        public AnalyticsMiddleware(RequestDelegate next, IAnalyticsCollector collector, ILogger<AnalyticsMiddleware> logger)
        {
            _next = next;
            _collector = collector;
            _logger = logger;
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
            catch (Exception ex)
            {
                statusCode = StatusCodes.Status500InternalServerError;
                _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
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
