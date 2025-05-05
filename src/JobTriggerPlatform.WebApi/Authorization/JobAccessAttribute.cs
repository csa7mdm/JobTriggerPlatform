using JobTriggerPlatform.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace JobTriggerPlatform.WebApi.Authorization;

/// <summary>
/// Attribute that ensures the user has access to the specified job.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class JobAccessAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobAccessAttribute"/> class.
    /// </summary>
    /// <param name="jobName">The name of the job to check access for.</param>
    public JobAccessAttribute(string jobName)
    {
        // Set the policy name to match the format used in AddJobAccessPolicy
        Policy = $"JobAccess:{jobName}";
    }
}
