using Microsoft.AspNetCore.Mvc;
using Mockups.Services.Analytics;

namespace Mockups.Controllers
{
    [ApiController]
    [Route("analytics")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> Summary(CancellationToken cancellationToken)
        {
            var summary = await _analyticsService.GetSummaryAsync(cancellationToken);
            return Ok(summary);
        }

        [HttpGet("usage")]
        public IActionResult Usage()
        {
            return Ok(_analyticsService.GetUsage());
        }

        [HttpGet("errors")]
        public IActionResult Errors()
        {
            return Ok(_analyticsService.GetErrors());
        }
    }
}
