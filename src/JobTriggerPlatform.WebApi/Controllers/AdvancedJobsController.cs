using JobTriggerPlatform.Application.Abstractions;
using JobTriggerPlatform.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobTriggerPlatform.WebApi.Controllers;

/// <summary>
/// Controller for advanced job management using declarative authorization.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdvancedJobsController : ControllerBase
{
    private readonly IEnumerable<IJobTriggerPlugin> _plugins;
    private readonly ILogger<AdvancedJobsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdvancedJobsController"/> class.
    /// </summary>
    /// <param name="plugins">The job trigger plugins.</param>
    /// <param name="logger">The logger.</param>
    public AdvancedJobsController(IEnumerable<IJobTriggerPlugin> plugins, ILogger<AdvancedJobsController> logger)
    {
        _plugins = plugins;
        _logger = logger;
    }

    /// <summary>
    /// Gets information about a sample job.
    /// </summary>
    /// <returns>Information about the sample job.</returns>
    [HttpGet("sample-job")]
    [JobAccess("SampleJob")]
    public IActionResult GetSampleJob()
    {
        var plugin = _plugins.FirstOrDefault(p => p.JobName == "SampleJob");
        if (plugin == null)
        {
            return NotFound("Sample job not found.");
        }

        return Ok(new
        {
            plugin.JobName,
            Parameters = plugin.Parameters
        });
    }

    /// <summary>
    /// Triggers the sample job.
    /// </summary>
    /// <param name="parameters">The job parameters.</param>
    /// <returns>The result of the job trigger.</returns>
    [HttpPost("sample-job/trigger")]
    [JobAccess("SampleJob")]
    public async Task<IActionResult> TriggerSampleJob([FromBody] Dictionary<string, string> parameters)
    {
        var plugin = _plugins.FirstOrDefault(p => p.JobName == "SampleJob");
        if (plugin == null)
        {
            return NotFound("Sample job not found.");
        }

        try
        {
            var result = await plugin.TriggerAsync(parameters);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering sample job");
            return StatusCode(500, "An error occurred while triggering the job.");
        }
    }

    /// <summary>
    /// Gets information about the advanced deployment job.
    /// </summary>
    /// <returns>Information about the advanced deployment job.</returns>
    [HttpGet("advanced-deployment")]
    [JobAccess("AdvancedDeployment")]
    public IActionResult GetAdvancedDeployment()
    {
        var plugin = _plugins.FirstOrDefault(p => p.JobName == "AdvancedDeployment");
        if (plugin == null)
        {
            return NotFound("Advanced deployment job not found.");
        }

        return Ok(new
        {
            plugin.JobName,
            Parameters = plugin.Parameters
        });
    }

    /// <summary>
    /// Triggers the advanced deployment job.
    /// </summary>
    /// <param name="parameters">The job parameters.</param>
    /// <returns>The result of the job trigger.</returns>
    [HttpPost("advanced-deployment/trigger")]
    [JobAccess("AdvancedDeployment")]
    public async Task<IActionResult> TriggerAdvancedDeployment([FromBody] Dictionary<string, string> parameters)
    {
        var plugin = _plugins.FirstOrDefault(p => p.JobName == "AdvancedDeployment");
        if (plugin == null)
        {
            return NotFound("Advanced deployment job not found.");
        }

        try
        {
            var result = await plugin.TriggerAsync(parameters);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering advanced deployment");
            return StatusCode(500, "An error occurred while triggering the job.");
        }
    }
}
