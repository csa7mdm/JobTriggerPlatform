using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace JobTriggerPlatform.Infrastructure.Authorization;

/// <summary>
/// Authorization requirement that checks if a user has access to a job based on the job name in the route.
/// </summary>
public class JobAccessFromRouteRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobAccessFromRouteRequirement"/> class.
    /// </summary>
    public JobAccessFromRouteRequirement()
    {
    }
}

/// <summary>
/// Handles the <see cref="JobAccessFromRouteRequirement"/> authorization requirement.
/// </summary>
public class JobAccessFromRouteHandler : AuthorizationHandler<JobAccessFromRouteRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<JobAccessFromRouteHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobAccessFromRouteHandler"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    /// <param name="logger">The logger.</param>
    public JobAccessFromRouteHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<JobAccessFromRouteHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        JobAccessFromRouteRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("No HTTP context available for job access check");
            return Task.CompletedTask;
        }

        var routeData = httpContext.GetRouteData();
        if (routeData == null || !routeData.Values.TryGetValue("name", out var nameValue) || nameValue == null)
        {
            _logger.LogWarning("No job name found in route for job access check");
            return Task.CompletedTask;
        }

        string jobName = nameValue.ToString()!;

        // Check if the user has a direct JobAccess claim for this job
        var hasJobAccessClaim = context.User.HasClaim("JobAccess", jobName);

        // Check if the user has one of the required roles for the job
        // Instead of using IAuthorizationService, directly check user claims
        var userRoles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        
        // This is a simplified check - in a real app, get the required roles from a database or config
        bool hasRequiredRole = false;
        
        // Simple default role mapping - in a real app, this would come from configuration
        if (jobName == "SampleJob" && (userRoles.Contains("Admin") || userRoles.Contains("Dev")))
        {
            hasRequiredRole = true;
        }
        else if (jobName == "AdvancedDeployment" && (userRoles.Contains("Admin")))
        {
            hasRequiredRole = true;
        }

        if (hasJobAccessClaim || hasRequiredRole)
        {
            context.Succeed(requirement);

            _logger.LogInformation("User {UserId} granted access to job {JobName} via route-based check",
                context.User.FindFirstValue(ClaimTypes.NameIdentifier), jobName);
            
            return Task.CompletedTask;
        }
        
        _logger.LogWarning("User {UserId} denied access to job {JobName} via route-based check",
            context.User.FindFirstValue(ClaimTypes.NameIdentifier), jobName);
            
        return Task.CompletedTask;
    }
}
