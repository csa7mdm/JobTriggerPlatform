using JobTriggerPlatform.Application.Abstractions;
using JobTriggerPlatform.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace JobTriggerPlatform.WebApi.Controllers;

/// <summary>
/// Controller for managing and triggering jobs.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly IEnumerable<IJobTriggerPlugin> _plugins;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<JobsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobsController"/> class.
    /// </summary>
    /// <param name="plugins">The job trigger plugins.</param>
    /// <param name="authorizationService">The authorization service.</param>
    /// <param name="logger">The logger.</param>
    public JobsController(
        IEnumerable<IJobTriggerPlugin> plugins, 
        IAuthorizationService authorizationService,
        ILogger<JobsController> logger)
    {
        _plugins = plugins;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all available job plugins.
    /// </summary>
    /// <returns>The list of job plugins.</returns>
    [HttpGet]
    [Authorize(Policy = "ViewDeploymentJobs")]
    public IActionResult GetJobs()
    {
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        
        var accessiblePlugins = _plugins
            .Where(p => p.RequiredRoles.Any(role => userRoles.Contains(role)))
            .Select(p => new
            {
                p.JobName,
                Parameters = p.Parameters
            });

        return Ok(accessiblePlugins);
    }

    /// <summary>
    /// Gets a specific job plugin by name.
    /// </summary>
    /// <param name="jobName">The job name.</param>
    /// <returns>The job plugin details.</returns>
    [HttpGet("{jobName}")]
    [Authorize(Policy = "ViewDeploymentJobs")]
    public async Task<IActionResult> GetJob(string jobName)
    {
        // Use our custom JobAccessRequirement for authorization
        var authorizationResult = await _authorizationService.AuthorizeAsync(
            User, null, new JobAccessRequirement(jobName));
            
        if (!authorizationResult.Succeeded)
        {
            return Forbid();
        }
        
        var plugin = _plugins.FirstOrDefault(p => p.JobName == jobName);
        if (plugin == null)
        {
            return NotFound("Job not found.");
        }

        return Ok(new
        {
            plugin.JobName,
            Parameters = plugin.Parameters
        });
    }

    /// <summary>
    /// Triggers a job.
    /// </summary>
    /// <param name="jobName">The job name.</param>
    /// <param name="parameters">The job parameters.</param>
    /// <returns>The result of the job trigger.</returns>
    [HttpPost("{jobName}/trigger")]
    [Authorize(Policy = "ManageDeploymentJobs")]
    public async Task<IActionResult> TriggerJob(string jobName, [FromBody] Dictionary<string, string> parameters)
    {
        // Use our custom JobAccessRequirement for authorization
        var authorizationResult = await _authorizationService.AuthorizeAsync(
            User, null, new JobAccessRequirement(jobName));
            
        if (!authorizationResult.Succeeded)
        {
            return Forbid();
        }
        
        var plugin = _plugins.FirstOrDefault(p => p.JobName == jobName);
        if (plugin == null)
        {
            return NotFound("Job not found.");
        }

        try
        {
            // Validate required parameters
            var missingParameters = plugin.Parameters
                .Where(p => p.IsRequired && (!parameters.ContainsKey(p.Name) || string.IsNullOrEmpty(parameters[p.Name])))
                .Select(p => p.Name)
                .ToList();

            if (missingParameters.Any())
            {
                return BadRequest($"Missing required parameters: {string.Join(", ", missingParameters)}");
            }

            _logger.LogInformation("Triggering job {JobName} with parameters: {@Parameters}", jobName, parameters);
            
            var result = await plugin.TriggerAsync(parameters);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering job {JobName}", jobName);
            return StatusCode(500, "An error occurred while triggering the job.");
        }
    }
}
