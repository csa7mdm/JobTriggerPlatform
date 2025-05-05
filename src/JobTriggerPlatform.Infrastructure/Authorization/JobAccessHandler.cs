using JobTriggerPlatform.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace JobTriggerPlatform.Infrastructure.Authorization;

/// <summary>
/// Handles the <see cref="JobAccessRequirement"/> authorization requirement.
/// </summary>
public class JobAccessHandler : AuthorizationHandler<JobAccessRequirement>
{
    private readonly IEnumerable<IJobTriggerPlugin> _plugins;
    private readonly ILogger<JobAccessHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobAccessHandler"/> class.
    /// </summary>
    /// <param name="plugins">The job trigger plugins.</param>
    /// <param name="logger">The logger.</param>
    public JobAccessHandler(IEnumerable<IJobTriggerPlugin> plugins, ILogger<JobAccessHandler> logger)
    {
        _plugins = plugins;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, JobAccessRequirement requirement)
    {
        // Check if the user has a claim for the specific job
        if (context.User.HasClaim(c => c.Type == "JobAccess" && c.Value == requirement.JobName))
        {
            _logger.LogInformation("User {UserId} has direct access to job {JobName} via claims", 
                context.User.FindFirstValue(ClaimTypes.NameIdentifier), requirement.JobName);
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Get user roles
        var userRoles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        
        // Find the plugin for the requested job
        var plugin = _plugins.FirstOrDefault(p => p.JobName == requirement.JobName);
        
        if (plugin == null)
        {
            _logger.LogWarning("Job {JobName} not found when checking access for user {UserId}", 
                requirement.JobName, context.User.FindFirstValue(ClaimTypes.NameIdentifier));
            return Task.CompletedTask;
        }

        // Check if any of the user's roles are in the plugin's required roles
        if (plugin.RequiredRoles.Any(role => userRoles.Contains(role)))
        {
            _logger.LogInformation("User {UserId} has access to job {JobName} via role membership", 
                context.User.FindFirstValue(ClaimTypes.NameIdentifier), requirement.JobName);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogInformation("User {UserId} denied access to job {JobName}. User roles: {UserRoles}, Required roles: {RequiredRoles}", 
                context.User.FindFirstValue(ClaimTypes.NameIdentifier), 
                requirement.JobName,
                string.Join(", ", userRoles),
                string.Join(", ", plugin.RequiredRoles));
        }

        return Task.CompletedTask;
    }
}
