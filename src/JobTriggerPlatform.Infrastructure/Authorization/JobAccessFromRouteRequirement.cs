using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

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
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<JobAccessFromRouteHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobAccessFromRouteHandler"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    /// <param name="authorizationService">The authorization service.</param>
    /// <param name="logger">The logger.</param>
    public JobAccessFromRouteHandler(
        IHttpContextAccessor httpContextAccessor,
        IAuthorizationService authorizationService,
        ILogger<JobAccessFromRouteHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        JobAccessFromRouteRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("No HTTP context available for job access check");
            return;
        }

        var routeData = httpContext.GetRouteData();
        if (routeData == null || !routeData.Values.TryGetValue("name", out var nameValue) || nameValue == null)
        {
            _logger.LogWarning("No job name found in route for job access check");
            return;
        }

        string jobName = nameValue.ToString()!;
        
        // Use the JobAccessRequirement to check if the user has access to the job
        var authorizationResult = await _authorizationService.AuthorizeAsync(
            context.User, null, new JobAccessRequirement(jobName));
            
        if (authorizationResult.Succeeded)
        {
            context.Succeed(requirement);
            
            _logger.LogInformation("User {UserId} granted access to job {JobName} via route-based check", 
                context.User.FindFirstValue(ClaimTypes.NameIdentifier), jobName);
        }
        else
        {
            _logger.LogWarning("User {UserId} denied access to job {JobName} via route-based check", 
                context.User.FindFirstValue(ClaimTypes.NameIdentifier), jobName);
        }
    }
}
