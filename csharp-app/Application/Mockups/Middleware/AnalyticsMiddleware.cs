using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mockups.Services.Analytics;

namespace Mockups.Middleware
{
    public class AnalyticsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAnalyticsCollector _collector;
        private readonly ILogger<AnalyticsMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public AnalyticsMiddleware(
            RequestDelegate next,
            IAnalyticsCollector collector,
            ILogger<AnalyticsMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next;
            _collector = collector;
            _logger = logger;
            _environment = environment;
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

                if (!context.Response.HasStarted)
                {
                    var detail = _environment.IsDevelopment() ? ex.ToString() : "An unexpected error occurred.";
                    var problemDetails = new ProblemDetails
                    {
                        Title = "Unhandled exception",
                        Status = StatusCodes.Status500InternalServerError,
                        Detail = detail,
                        Instance = context.Request.Path
                    };

                    problemDetails.Extensions["traceId"] = context.TraceIdentifier;

                    context.Response.Clear();
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsJsonAsync(problemDetails);
                }
                else
                {
                    throw;
                }
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
