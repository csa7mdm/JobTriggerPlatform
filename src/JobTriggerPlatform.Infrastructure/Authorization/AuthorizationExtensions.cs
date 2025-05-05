using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace JobTriggerPlatform.Infrastructure.Authorization;

/// <summary>
/// Extension methods for registering authorization services.
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Adds job access policies and handlers to the authorization services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddJobAccessPolicies(this IServiceCollection services)
    {
        // Register the authorization handler
        services.AddScoped<IAuthorizationHandler, JobAccessHandler>();

        return services;
    }

    /// <summary>
    /// Adds a custom job access policy.
    /// </summary>
    /// <param name="options">The authorization options.</param>
    /// <param name="jobName">The name of the job to create a policy for.</param>
    /// <returns>The authorization options.</returns>
    public static AuthorizationOptions AddJobAccessPolicy(this AuthorizationOptions options, string jobName)
    {
        options.AddPolicy($"JobAccess:{jobName}", policy =>
            policy.Requirements.Add(new JobAccessRequirement(jobName)));
        
        return options;
    }
}
